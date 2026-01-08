using System;
using System.IO;
using Microsoft.Web.XmlTransform;

namespace ConfigTransformExtension.Services
{
    public class XmlTransformService
    {
        public bool ApplyTransform(string sourceFile, string transformFile, string destinationFile)
        {
            try
            {
                if (!File.Exists(sourceFile))
                {
                    throw new FileNotFoundException($"Arquivo de origem não encontrado: {sourceFile}");
                }

                if (!File.Exists(transformFile))
                {
                    throw new FileNotFoundException($"Arquivo de transformação não encontrado: {transformFile}");
                }

                using (var document = new XmlTransformableDocument())
                {
                    document.PreserveWhitespace = true;
                    document.Load(sourceFile);

                    using (var transform = new XmlTransformation(transformFile))
                    {
                        if (transform.Apply(document))
                        {
                            document.Save(destinationFile);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao aplicar transformação: {ex.Message}", ex);
            }
        }

        public string FindBaseConfigFile(string transformFilePath)
        {
            string directory = Path.GetDirectoryName(transformFilePath);
            string fileName = Path.GetFileName(transformFilePath);

            // Padrões: web.pestana-hlg2.config, web.pestana-tst1.config, app.pestana-prd.config, etc.
            string[] possibleBaseFiles = new[]
            {
                "web.config",
                "Web.config",
                "app.config",
                "App.config"
            };

            foreach (var baseFile in possibleBaseFiles)
            {
                string baseFilePath = Path.Combine(directory, baseFile);
                if (File.Exists(baseFilePath))
                {
                    return baseFilePath;
                }
            }

            return null;
        }

        public string GetEnvironmentName(string transformFilePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(transformFilePath);
            
            // Extrai o nome do ambiente de padrões como "web.pestana-hlg2" ou "app.pestana-tst1"
            string[] parts = fileName.Split('-');
            if (parts.Length > 1)
            {
                return parts[parts.Length - 1].ToUpper();
            }

            return "DESCONHECIDO";
        }
    }
}
