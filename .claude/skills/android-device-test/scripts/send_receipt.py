"""Send receipt requests to the launcher on a device through the PosSystemAPI activity intent.

Each request is sent with `am start` to eu.fiskaltrust.androidlauncher.PosSystemAPI; the response is
read back from logcat (tag PosSystemAPI, line "Finishing with response: ..."). Request and response
are written side by side to <out>/<name>.json and a one-line summary is printed.

Usage:
    python -I send_receipt.py [--out DIR] [--keep-moment] [--timeout SEC] <file.json|dir|zero|daily|init> ...

Environment:
    ADB_SERIAL       device serial, e.g. 192.168.1.188:45587 (required)
    FT_CASHBOX_ID    cashbox id (required)
    FT_ACCESS_TOKEN  cashbox access token (required)
    ADB              path to adb (optional; default: adb on PATH, then the Android SDK)

Request files may use the placeholder "{{ ftCashBoxID }}"; it is replaced. cbReceiptMoment is set to
now unless --keep-moment is given. cbReceiptReference (and cbPreviousReceiptReference) get a run
suffix so references stay unique across runs but still match each other within one run. A response
counts only if its cbReceiptReference matches the request's; if a request times out, the remaining ones
are not sent, as the Queue would process them behind it anyway. The cashbox id is replaced with the
placeholder in both request and response before the result is written.
"""
import argparse, base64, datetime, json, os, re, shutil, subprocess, sys, time, uuid

sys.stdout.reconfigure(encoding="utf-8")

PL = 0x504C_2000_0000_0000
SHORTCUTS = {"zero": 0x2000, "daily": 0x2011, "init": 0x4001}
KEEP_CAPTIONS = ("FAILURE", "Numer dokumentu fiskalnego", "Numer raportu dobowego")


def find_adb():
    if os.environ.get("ADB"):
        return os.environ["ADB"]
    on_path = shutil.which("adb")
    if on_path:
        return on_path
    for root in (os.environ.get("ANDROID_HOME"), os.environ.get("ANDROID_SDK_ROOT"),
                 os.path.join(os.environ.get("LOCALAPPDATA", ""), "Android", "Sdk"),
                 os.path.expanduser("~/Library/Android/sdk"), os.path.expanduser("~/Android/Sdk")):
        if root:
            exe = os.path.join(root, "platform-tools", "adb.exe" if os.name == "nt" else "adb")
            if os.path.exists(exe):
                return exe
    sys.exit("adb not found: set ADB or put it on PATH")


def require(name):
    value = os.environ.get(name)
    if not value:
        sys.exit(f"{name} is not set")
    return value


def shortcut(kind):
    """A minimal PL request for the receipt cases that need no body (other markets: use a file)."""
    return {"cbTerminalID": "1", "cbReceiptReference": kind.upper(), "cbChargeItems": [], "cbPayItems": [],
            "ftReceiptCase": PL | SHORTCUTS[kind], "cbUser": "Admin", "Currency": "PLN"}


def expand(args):
    for arg in args:
        if arg in SHORTCUTS:
            yield arg, shortcut(arg)
        elif os.path.isdir(arg):
            for name in sorted(os.listdir(arg)):
                if name.endswith(".json"):
                    yield from expand([os.path.join(arg, name)])
        else:
            with open(arg, encoding="utf-8") as f:
                yield os.path.splitext(os.path.basename(arg))[0], json.load(f)


def response_for(line, reference):
    """The parsed response in a logcat line if it belongs to the request with this reference.

    A line without a JSON receipt response (the launcher failed before the Queue answered, e.g. a wrong
    access token) is taken as is: it has no reference to match on.
    """
    _, sep, content = line.partition(" - Success - ")
    if not sep:
        return {"raw": line}
    try:
        response = json.loads(content)
    except json.JSONDecodeError:
        # logcat truncates long entries (~4 KB); keep the cut-off response if it is ours
        return {"raw": line} if f'"{reference}"' in content else None
    return response if response.get("cbReceiptReference") == reference else None


def main():
    p = argparse.ArgumentParser()
    p.add_argument("requests", nargs="+")
    p.add_argument("--out", default="device-test-results")
    p.add_argument("--keep-moment", action="store_true")
    p.add_argument("--timeout", type=int, default=240, help="seconds to wait for each response")
    a = p.parse_args()

    adb, serial = find_adb(), require("ADB_SERIAL")
    cashbox, token = require("FT_CASHBOX_ID"), require("FT_ACCESS_TOKEN")
    if ":" in serial:
        subprocess.run([adb, "connect", serial], capture_output=True)
    run = datetime.datetime.now().strftime("%H%M%S")
    os.makedirs(a.out, exist_ok=True)
    b64 = lambda s: base64.urlsafe_b64encode(s.encode()).decode()
    redact = lambda s: re.sub(re.escape(cashbox), "{{ ftCashBoxID }}", s, flags=re.IGNORECASE)

    for name, body in expand(a.requests):
        body["ftCashBoxID"] = cashbox
        body.pop("ftPosSystemId", None)
        if not a.keep_moment or "cbReceiptMoment" not in body:
            body["cbReceiptMoment"] = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
        for key in ("cbReceiptReference", "cbPreviousReceiptReference"):
            if isinstance(body.get(key), str):
                body[key] = f"{body[key]}-{run}"
        headers = {"x-cashbox-id": cashbox, "x-cashbox-accesstoken": token,
                   "x-operation-id": str(uuid.uuid4()), "Content-Type": "application/json"}

        subprocess.run([adb, "-s", serial, "logcat", "-c"], capture_output=True)
        subprocess.run([adb, "-s", serial, "shell", "am", "start", "-W",
                        "-n", "eu.fiskaltrust.androidlauncher/eu.fiskaltrust.androidlauncher.PosSystemAPI",
                        "--es", "Method", "POST", "--es", "Path", "/v2/sign",
                        "--es", "HeaderJsonObjectBase64Url", b64(json.dumps(headers)),
                        "--es", "BodyBase64Url", b64(json.dumps(body))], capture_output=True)

        response, other, deadline = None, None, time.time() + a.timeout
        while response is None and time.time() < deadline:
            log = subprocess.run([adb, "-s", serial, "logcat", "-d", "-s", "PosSystemAPI"],
                                 capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
            for l in log.splitlines():
                if "Finishing with response: " in l:
                    line = l.split("Finishing with response: ", 1)[1]
                    response = response_for(line, body.get("cbReceiptReference"))
                    if response is not None:
                        break
                    other = line  # the response to an earlier request that timed out
            else:
                time.sleep(2)

        timed_out = response is None
        if timed_out:
            response = {"raw": f"no response within {a.timeout}s" + (f"; last other response: {other}" if other else "")}
        with open(os.path.join(a.out, f"{name}.json"), "w", encoding="utf-8") as f:
            # keep result files shareable
            f.write(redact(json.dumps({"request": body, "response": response}, indent=2, ensure_ascii=False)))

        if timed_out:
            print(f"{name}: NO RESPONSE | {redact(response['raw'])[:300]}")
            print("Stopping: the request may still be pending in the Queue; check the device before sending more.")
            sys.exit(1)
        if "raw" in response:
            print(f"{name}: NO VALID RESPONSE | {redact(response['raw'])[:300]}")
            continue
        state = response.get("ftState", 0)
        verdict = "ERROR" if state & 0xFFFF_FFFF == 0xEEEE_EEEE else "ok"
        sigs = "; ".join(f"{s['Caption']}={s['Data'][:160]}" for s in response.get("ftSignatures", [])
                         if s["Caption"] in KEEP_CAPTIONS or s["Caption"].startswith("[")
                         or "nie wydrukowane" in s["Caption"] or "not printed" in s["Caption"].lower())
        extra = f" | ftStateData={json.dumps(response['ftStateData'], ensure_ascii=False)[:400]}" if response.get("ftStateData") else ""
        print(f"{name}: {hex(state)} {verdict} | {sigs}{extra}")
        time.sleep(4)  # let the printer finish before the next request


if __name__ == "__main__":
    main()
