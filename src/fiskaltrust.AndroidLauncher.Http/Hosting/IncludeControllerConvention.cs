using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    class IncludeControllerConvention<T> : IApplicationModelConvention where T : Controller
    {
        public void Apply(ApplicationModel application)
        {
            var includedController = application.Controllers.FirstOrDefault((model) => !model.ControllerType.IsEquivalentTo(typeof(T)));

            if(includedController != null)
            {                
                for (int i = 0; i < application.Controllers.Count; i++)
                {
                    if (application.Controllers[i] != includedController)
                        application.Controllers.RemoveAt(i);
                }
            }
        }
    }
}