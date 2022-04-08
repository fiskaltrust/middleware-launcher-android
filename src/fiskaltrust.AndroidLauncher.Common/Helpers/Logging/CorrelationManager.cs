﻿using System;
using System.Runtime.Remoting.Messaging;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    public static class CorrelationManager
    {
        private const string OperationIdKey = "OperationId";

        public static void SetOperationId(string operationId)
        {
            CallContext.LogicalSetData(OperationIdKey, operationId);
        }

        public static string GetOperationId()
        {
            var id = CallContext.LogicalGetData(OperationIdKey) as string;
            return id ?? Guid.NewGuid().ToString();
        }
    }
}
