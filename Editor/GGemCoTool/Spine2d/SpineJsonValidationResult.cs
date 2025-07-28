using System.Collections.Generic;

namespace GGemCo2DCoreEditor
{
    public class SpineJsonValidationResult
    {
        public string FilePath;
        public List<ValidationError> Errors = new();

        public class ValidationError
        {
            public string EventName;
            public string OriginalValue;
            public string ErrorMessage;
            public string JsonPath;
        }
    }
}