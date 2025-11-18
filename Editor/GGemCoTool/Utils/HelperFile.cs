using System.IO;

namespace GGemCo2DCoreEditor
{
    public static class HelperFile
    {
        public static void CopyDirectory(string sourceDir, string targetDir, bool copyMetafile = true)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                if (!copyMetafile && file.EndsWith(".meta")) continue;

                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                string targetSubDir = Path.Combine(targetDir, dirName);
                CopyDirectory(directory, targetSubDir, copyMetafile);
            }
        }
    }
}