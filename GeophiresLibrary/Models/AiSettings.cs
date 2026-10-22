using System;
using System.Collections.Generic;
using System.Text;

namespace GeophiresLibrary.Models
{
    public sealed class AiSettings
    {
        public const string StorageKey = "geophires.openai.key";
        public string OpenAIApiKey { get; set; } = "";
        public string Model { get; set; } = "gpt-5.4-mini";
    }
}
