﻿using System.Collections.Generic;

namespace HpToolsLauncher.RTS
{
    /// <summary>
    /// Model class which contains the script name and runtime settings (currently additional attributes)
    /// </summary>
    public class ScriptRTSModel
    {
        private string scriptName;
        private List<AdditionalAttributeModel> additionalAttributes;

        public ScriptRTSModel(string scriptName)
        {
            this.scriptName = scriptName;
            additionalAttributes = new List<AdditionalAttributeModel>();
        }

        public string GetScriptName()
        {
            return scriptName;
        }
        public void AddAdditionalAttribute(AdditionalAttributeModel additionalAttribute)
        {
            additionalAttributes.Add(additionalAttribute);
        }

        /**
         * Convert additional attribute model to key value struct
         */
        public List<RTSHelper.KeyValuePair> GetKeyValuePairs()
        {
            List<RTSHelper.KeyValuePair> keyValuePairs = new List<RTSHelper.KeyValuePair>();

            foreach (AdditionalAttributeModel additionalAttribute in additionalAttributes)
            {
                keyValuePairs.Add(
                    new RTSHelper.KeyValuePair(
                        additionalAttribute.GetName(),
                        additionalAttribute.GetValue()
                    )
                );
                keyValuePairs.Add(
                    new RTSHelper.KeyValuePair(
                        "~" + additionalAttribute.GetName(),
                        additionalAttribute.GetDescription()
                    )
                );
            }

            return keyValuePairs;
        }
    }
}
