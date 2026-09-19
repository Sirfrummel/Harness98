using System;
using System.Collections;
using System.IO;
using Harness98;

public sealed class Updater
{
    public static int Main(string[] args)
    {
        string root = AppDomain.CurrentDomain.BaseDirectory;
        string stage = Path.Combine(root, "UPDATE-STAGE");
        string marker = Path.Combine(stage, "READY.TAG");
        if (!File.Exists(marker)) return 0;

        Console.WriteLine("Applying the staged Harness98 update...");
        try
        {
            UpdateManifest manifest = UpdateManifest.Load(
                Path.Combine(stage, "MANIFEST.INI"));
            VerifyStage(stage, manifest);
            Apply(root, stage, manifest);
            File.Delete(marker);
            Console.WriteLine("Harness98 " + manifest.Version + " is ready.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("UPDATE FAILED: " + ex.Message);
            Console.WriteLine("The staged update was retained for another attempt.");
            return 1;
        }
    }

    public static void VerifyStage(string stage, UpdateManifest manifest)
    {
        for (int i = 0; i < manifest.Files.Count; i++)
        {
            UpdateFile file = (UpdateFile)manifest.Files[i];
            string path = Path.Combine(stage, file.Name);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Staged file is missing: " + file.Name);
            }
            string actual = UpdateManifest.HashFile(path);
            if (String.Compare(actual, file.Sha256, true) != 0)
            {
                throw new ApplicationException("Hash check failed for " + file.Name + ".");
            }
        }
    }

    public static void Apply(string root, string stage, UpdateManifest manifest)
    {
        string backup = Path.Combine(root, "UPDATE-BACKUP");
        RecreateDirectory(backup);
        ArrayList existed = new ArrayList();
        ArrayList replaced = new ArrayList();
        try
        {
            for (int i = 0; i < manifest.Files.Count; i++)
            {
                UpdateFile file = (UpdateFile)manifest.Files[i];
                string target = Path.Combine(root, file.Name);
                bool present = File.Exists(target);
                existed.Add(present);
                if (present)
                {
                    File.Copy(target, Path.Combine(backup, file.Name), true);
                }
            }
            for (int i = 0; i < manifest.Files.Count; i++)
            {
                UpdateFile file = (UpdateFile)manifest.Files[i];
                string target = Path.Combine(root, file.Name);
                replaced.Add(file.Name);
                File.Copy(Path.Combine(stage, file.Name), target, true);
            }
        }
        catch
        {
            for (int i = 0; i < replaced.Count; i++)
            {
                string name = (string)replaced[i];
                string saved = Path.Combine(backup, name);
                if ((bool)existed[i])
                {
                    File.Copy(saved, Path.Combine(root, name), true);
                }
                else
                {
                    string target = Path.Combine(root, name);
                    if (File.Exists(target)) File.Delete(target);
                }
            }
            throw;
        }
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
        Directory.CreateDirectory(path);
    }
}
