using System;
using whatisthatService.SpeciesIndentifier.Wolfram.Response.Dto;

namespace whatisthatService.SpeciesIndentifier.Wolfram.Response
{
    class WolframCommonNameData
    {
        public readonly static WolframCommonNameData NULL = new WolframCommonNameData(null);
        private readonly String _name;

        public string Name
        {
            get { return _name; }
        }

        public static WolframCommonNameData GetInstance(WolframResponseDto dto)
        {
            return (dto == null || dto.Success == null || dto.Success.ToLower() != "true") ? NULL : new WolframCommonNameData(dto);
        }

        private WolframCommonNameData(WolframResponseDto dto)
        {
            var name = dto != null ? dto.Result.Replace("\"", "") : "";
            _name = String.Equals("Missing[NotAvailable]", name, StringComparison.InvariantCultureIgnoreCase)
                ? ""
                : name;
        }
    }
}
