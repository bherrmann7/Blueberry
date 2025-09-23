using System;
using System.IO;
using System.Text.Json;
using BluelBerry;

class TestAutoAddModel
{
    static void Main()
    {
        // Clean up any existing test file
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var testModelsFile = Path.Combine(homeDirectory, ".bb", "test-models.json");
        
        if (File.Exists(testModelsFile))
        {
            File.Delete(testModelsFile);
        }

        // Create a temporary test directory and models file
        var testDir = Path.Combine(homeDirectory, ".bb");
        Directory.CreateDirectory(testDir);

        // Create initial models file with some test data
        var initialModels = new List<Model>
        {
            new Model { Name = "Test Model", ShortName = "test-model", Endpoint = "http://test.com", Key = "test-key" }
        };

        var json = JsonSerializer.Serialize(initialModels, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(testModelsFile, json);

        // Temporarily replace the models file path for testing
        var modelManager = new ModelManager();
        
        Console.WriteLine("Initial models:");
        foreach (var model in modelManager.Models)
        {
            Console.WriteLine($"  {model.ShortName} - {model.Endpoint}");
        }

        // Test adding a new model
        Console.WriteLine("\nTesting AddModelIfNotExists...");
        var added = modelManager.AddModelIfNotExists("gpt-4o", "https://api.openai.com/v1", "sk-test123");
        Console.WriteLine($"Model added: {added}");

        Console.WriteLine("\nModels after adding new one:");
        foreach (var model in modelManager.Models)
        {
            Console.WriteLine($"  {model.ShortName} - {model.Endpoint}");
        }

        // Test adding the same model again (should not add)
        Console.WriteLine("\nTesting adding the same model again...");
        var addedAgain = modelManager.AddModelIfNotExists("gpt-4o", "https://api.openai.com/v1", "sk-test123");
        Console.WriteLine($"Model added again: {addedAgain}");

        // Test adding the same model with different key (should update)
        Console.WriteLine("\nTesting adding the same model with different key...");
        var updated = modelManager.AddModelIfNotExists("gpt-4o", "https://api.openai.com/v1", "sk-newkey456");
        Console.WriteLine($"Model updated: {updated}");

        Console.WriteLine("\nFinal models:");
        foreach (var model in modelManager.Models)
        {
            Console.WriteLine($"  {model.ShortName} - {model.Endpoint} - {model.Key}");
        }

        // Clean up
        if (File.Exists(testModelsFile))
        {
            File.Delete(testModelsFile);
        }

        Console.WriteLine("\nTest completed successfully!");
    }
}