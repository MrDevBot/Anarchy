﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace Discord
{
    public class ApplicationCommandProperties
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        public bool ShouldSerializeName() => Name != null;


        [JsonProperty("description")]
        public string Description { get; set; }

        public bool ShouldSerializeDescription() => Description != null;


        [JsonProperty("options")]
        public List<ApplicationCommandOption> Options { get; set; }

        public bool ShouldSerializeOptions() => Options != null;
    }
}