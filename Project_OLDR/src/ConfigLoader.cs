using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Windows.Forms;
using Project_OLDR.src;

namespace ProjectOLDR
{
    public static class ConfigLoader
    {
        private static readonly XmlDocument _doc;

        static ConfigLoader()
        {
            _doc = new XmlDocument();

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = null;

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith("EmbeddedConfig.xml", StringComparison.OrdinalIgnoreCase))
                {
                    resourceName = name;
                    break;
                }
            }

            if (resourceName == null)
            {
                MessageBox.Show("Erreur : EmbeddedConfig.xml not found in resources !");
                return;
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string rawContent = reader.ReadToEnd().Trim();

                try
                {
                    string secretKey = "Hideforpublicrepo";

                    string decryptedXml;
                    if (rawContent.StartsWith("<?xml"))
                    {
                        decryptedXml = rawContent;
                    }
                    else
                    {
                        decryptedXml = OpenSsl.DecryptString(rawContent, secretKey);
                    }

                    _doc.LoadXml(decryptedXml);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error ConfigLoader / OpenSSL : " + ex.Message, "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static string Get(string key)
        {
            try
            {
                var node = _doc.SelectSingleNode($"/configuration/appSettings/add[@key='{key}']");
                return node?.Attributes?["value"]?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
