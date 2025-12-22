using Microsoft.AspNetCore.Mvc.Routing;
using System.Text;

namespace LoanApi.Controllers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ModuleVersionedRouteAttribute : Attribute, IRouteTemplateProvider
    {
        private int? _order;
        private readonly string _moduleName;
        private readonly string? _submoduleName;

        public string? Template
        {
            get
            {
                var template = new StringBuilder()
                    .Append("api/v{version:apiVersion}/")
                    .Append(_moduleName);

                if (!string.IsNullOrWhiteSpace(_submoduleName))
                {
                    template.Append('/').Append(_submoduleName);
                }

                template.Append("/[controller]");
                return template.ToString();
            }
        }

        public int? Order
        {
            get => _order ?? 0;
            set => _order = value;
        }

        public string? Name { get; set; }

        public ModuleVersionedRouteAttribute(string moduleName, string? subModuleName = null)
        {
            _moduleName = moduleName;
            _submoduleName = subModuleName;
        }
    }
}
