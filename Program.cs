using System.Diagnostics;
using Google.Cloud.Firestore;
using Newtonsoft.Json;

class Program
{
    static async Task Main(string[] args)
    {

        var projectId = "";
        var credentialsPath = "";
        var documentPath = "";

        foreach (var arg in args)
        {
            if (arg.StartsWith("--project-id"))
            {
                projectId = GetArgumentValue(arg);
            }
            else if (arg.StartsWith("--credentials"))
            {
                credentialsPath = GetArgumentValue(arg);
            }
            else if (arg.StartsWith("--document-path"))
            {
                documentPath = GetArgumentValue(arg);
            }
        }
        if (projectId == "" || credentialsPath == "" || documentPath == "")
        {
            Console.WriteLine("Usage: --project-id={PROJECT_ID} --credentials={CREDENTIALS_PATH} --document-path={DOCUMENT_PATH} [--upload={FILE_PATH}] [--download]");
            return;
        }

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
        var firestoreDb = FirestoreDb.Create(projectId);
        var firestoreService = new FirestoreService(firestoreDb);

        //Upload JSON to Firestore "--upload={FILE_PATH}"
        if (args.Any(arg => arg.StartsWith("--upload")))
        {
            var filePath = GetArgumentValue(args.First(arg => arg.StartsWith("--upload")));
            var json = File.ReadAllText(filePath);
            Console.WriteLine(json);
            var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? throw new Exception("Invalid JSON");
            await firestoreService.SetDocumentAsync(documentPath, payload);
        }
        else
        {
            var json = await firestoreService.GetJSONAsync(documentPath);
            Console.WriteLine(json);
        }
    }

    static string GetArgumentValue(string arg)
    {
        return arg.Split('=')[1].Trim();
    }
}

public class FirestoreService
{
    private FirestoreDb firestoreDb;

    public FirestoreService(FirestoreDb firestoreDb)
    {
        this.firestoreDb = firestoreDb;
    }

    public async Task<Dictionary<string, object>> GetDocumentAsync(string path)
    {
        var document = await firestoreDb.Document(path).GetSnapshotAsync();
        return document.ToDictionary();
    }


    public async Task SetDocumentAsync(string path, Dictionary<string, object> payload)
    {
        //count the number of "/" in the path to determine if it is a collection or document
        var isCollection = path.Split('/').Length % 2 == 1;
        if (isCollection)
        {
            var collection = firestoreDb.Collection(path);
            //create file
            var document = collection.Document();
            await document.SetAsync(payload);
        }
        else
        {
            var document = firestoreDb.Document(path);
            await document.SetAsync(payload);
        }
    }

    public async Task<string> GetJSONAsync(string path)
    {
        var document = await GetDocumentAsync(path);
        return JsonConvert.SerializeObject(document);
    }
}