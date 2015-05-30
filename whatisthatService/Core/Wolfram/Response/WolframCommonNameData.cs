using System;
using whatisthatService.Core.Wolfram.Response.Dto;

namespace whatisthatService.Core.Wolfram.Response
{
    public class WolframCommonNameData
    {
        public readonly static WolframCommonNameData NULL = new WolframCommonNameData("");
        private readonly String _name;

        public string Name
        {
            get { return _name; }
        }

        public static WolframCommonNameData GetInstance(WolframResponseDto dto)
        {
            if (dto == null || dto.Success == null || dto.Success.ToLower() != "true")
            {
                return NULL;
            }

            var rawName = dto.Result.Replace("\"", "");
            var name = String.Equals("Missing[NotAvailable]", rawName, StringComparison.InvariantCultureIgnoreCase) ? "" : rawName;
            return GetInstance(name);
        }

        public static WolframCommonNameData GetInstance(String name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return NULL;
            }

            return new WolframCommonNameData(name);
        }

        private WolframCommonNameData(String name)
        {
            _name = name;
        }
    }
}
