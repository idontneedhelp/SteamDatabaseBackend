﻿/*
 * Copyright (c) 2013-present, SteamDB. All rights reserved.
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE file.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SteamDatabaseBackend
{
    static class LocalConfig
    {
        private static readonly JsonSerializerSettings JsonFormatted = new JsonSerializerSettings { Formatting = Formatting.Indented };

        [JsonObject(MemberSerialization.OptIn)]
        public class CDNAuthToken
        {
            [JsonProperty]
            public string Token { get; set; }

            [JsonProperty]
            public DateTime Expiration { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class LocalConfigJson
        {
            [JsonProperty]
            public uint CellID { get; set; }

            [JsonProperty]
            public uint ChangeNumber { get; set; }

            [JsonProperty]
            public string SentryFileName { get; set; }

            [JsonProperty]
            public byte[] Sentry { get; set; }

            [JsonProperty]
            public string LoginKey { get; set; }

            [JsonProperty]
            public ConcurrentDictionary<uint, CDNAuthToken> CDNAuthTokens { get; set; }

            [JsonProperty]
            public Dictionary<uint, uint> FreeLicensesToRequest { get; set; }

            public LocalConfigJson()
            {
                CDNAuthTokens = new ConcurrentDictionary<uint, CDNAuthToken>();
                FreeLicensesToRequest = new Dictionary<uint, uint>();
            }
        }

        private static readonly string ConfigPath = Path.Combine(Application.Path, "files", ".support", "localconfig.json");

        public static LocalConfigJson Current { get; private set; } = new LocalConfigJson();

        public static void Load()
        {
            if (File.Exists(ConfigPath))
            {
                Current = JsonConvert.DeserializeObject<LocalConfigJson>(File.ReadAllText(ConfigPath));

                var time = DateTime.Now;
                var removed = 0;

                foreach (var token in Current.CDNAuthTokens)
                {
                    if (time > token.Value.Expiration)
                    {
                        Current.CDNAuthTokens.TryRemove(token.Key, out _);
                        removed++;
                    }
                }

                if (removed > 0)
                {
                    Log.WriteInfo("Local Config", $"Removing {removed} expired depot tokens");
                }
            }

            if (Current.FreeLicensesToRequest.Count > 0)
            {
                Log.WriteInfo("Local Config", $"There are {Current.FreeLicensesToRequest.Count} free licenses to request");
            }

            Save();
        }

        public static void Save()
        {
            Log.WriteDebug("Local Config", "Saving...");

            var data = JsonConvert.SerializeObject(Current, JsonFormatted);

            lock (ConfigPath)
            {
                File.WriteAllText(ConfigPath, data);
            }
        }
    }
}
