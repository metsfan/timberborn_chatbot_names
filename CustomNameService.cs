#nullable enable

using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CustomNameList
{
    class CustomNameService {
        private readonly string _textFile;

        internal bool IsInitialized { get; private set; }

        public CustomNameService(string textFile) {
            _textFile = textFile;
        }

        internal void Init() {
            if (!File.Exists(_textFile)) {
                Plugin.Log.LogError($"Could not find names file at: {_textFile}");
                Plugin.Log.LogWarning($"Will use standard game names instead.");
                return;
            }

            IsInitialized = true;
        }

        internal String? NextName()
        {
            var allNames = File.ReadAllLines(_textFile).ToList().Select(e => e.Trim().Replace("\r", "")).ToList();
            if (allNames.Count == 0)
            {
                return null;
            }
            
            var nextName = allNames[0];
            allNames.RemoveAt(0);
            
            File.WriteAllLines(_textFile, allNames);
            
            return nextName;
        }
    }
}