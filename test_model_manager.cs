using System;
using System.IO;
using System.Collections.Generic;
using BluelBerry;

class TestModelManager
{
    static void Main()
    {
        Console.WriteLine("Testing ModelManager.AddModelIfNotExists...");
        
        var modelManager = new ModelManager();
        
        Console.WriteLine("\nCurrent models:");
        foreach (var model in modelManager.Models)
        {
            Console.WriteLine($"  {model.ShortName} -> {model.Endpoint}");
        }
        
        // Test adding a new model
        Console.WriteLine("\nTesting adding a new model: 'test-model-auto' with endpoint 'https://api.test.com/v1'");
        var added = modelManager.AddModelIfNotExists("test-model-auto", "https://api.test.com/v1", "sk-test-key-123");
        Console.WriteLine($"Model added: {added}");
        
        Console.WriteLine("\nModels after adding:");
        foreach (var model in modelManager.Models)
        {
            Console.WriteLine($"  {model.ShortName} -> {model.Endpoint}");
        }
        
        // Test adding the same model again
        Console.WriteLine("\nTesting adding the same model again...");
        var addedAgain = modelManager.AddModelIfNotExists("test-model-auto", "https://api.test.com/v1", "sk-test-key-123");
        Console.WriteLine($"Model added again: {addedAgain}");
        
        // Test adding same model with different key
        Console.WriteLine("\nTesting adding same model with different key...");
        var updated = modelManager.AddModelIfNotExists("test-model-auto", "https://api.test.com/v1", "sk-new-key-456");
        Console.WriteLine($"Model updated: {updated}");
        
        Console.WriteLine("\nFinal test complete.");
    }
}