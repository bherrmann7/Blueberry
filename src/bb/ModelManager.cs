using System.Text.Json;

namespace BlueBerry;

public class ModelManager
{
    private readonly string _modelsFilePath;
    private List<Model> _models = new();

    public ModelManager()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _modelsFilePath = Path.Combine(homeDirectory, ".bb", "models.json");
        LoadModels();
    }

    public List<Model> Models => _models;

    public Model? GetModelByShortName(string shortName)
    {
        return _models.FirstOrDefault(m => m.ShortName.Equals(shortName, StringComparison.OrdinalIgnoreCase));
    }

    public string? GetApiModelName(string shortName)
    {
        var model = GetModelByShortName(shortName);
        return model?.Name;
    }

    public List<string> GetAvailableModelNames()
    {
        return _models.Select(m => m.ShortName).ToList();
    }

    public bool AddModelIfNotExists(string modelName, string endpoint, string key)
    {
        // Check if this exact combination already exists
        var existingModel = _models.FirstOrDefault(m => 
            m.ShortName.Equals(modelName, StringComparison.OrdinalIgnoreCase) &&
            m.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase));

        if (existingModel != null)
        {
            // Update the key if it's different
            if (!existingModel.Key.Equals(key, StringComparison.Ordinal))
            {
                existingModel.Key = key;
                SaveModels();
                Console.WriteLine($"🔑 Updated key for existing model: {existingModel.ShortName} ({existingModel.Endpoint})");
                return true;
            }
            return false; // No changes needed
        }

        // Create a new model entry
        var newModel = new Model
        {
            Name = GenerateModelDisplayName(modelName, endpoint),
            ShortName = modelName,
            Endpoint = endpoint,
            Key = key
        };

        _models.Add(newModel);
        SaveModels();
        
        Console.WriteLine($"✅ Added new model configuration: {newModel.ShortName} ({newModel.Endpoint})");
        return true;
    }

    private string GenerateModelDisplayName(string modelName, string endpoint)
    {
        // Try to create a friendly display name based on the model name and endpoint
        var displayName = modelName;
        
        if (endpoint.Contains("openai.com"))
        {
            displayName = $"{modelName} (OpenAI)";
        }
        else if (endpoint.Contains("anthropic.com"))
        {
            displayName = $"{modelName} (Anthropic)";
        }
        else if (endpoint.Contains("cerebras.ai"))
        {
            displayName = $"{modelName} (Cerebras)";
        }
        else if (endpoint.Contains("localhost") || endpoint.Contains("127.0.0.1"))
        {
            displayName = $"{modelName} (Local)";
        }
        else
        {
            // Extract domain for other providers
            try
            {
                var uri = new Uri(endpoint);
                var domain = uri.Host;
                if (domain.StartsWith("api."))
                {
                    domain = domain.Substring(4);
                }
                if (domain.EndsWith(".com"))
                {
                    domain = domain.Substring(0, domain.Length - 4);
                }
                displayName = $"{modelName} ({char.ToUpper(domain[0])}{domain.Substring(1)})";
            }
            catch
            {
                displayName = $"{modelName} (Custom)";
            }
        }

        return displayName;
    }

    private void SaveModels()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_models, options);
            Directory.CreateDirectory(Path.GetDirectoryName(_modelsFilePath)!);
            File.WriteAllText(_modelsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not save models to {_modelsFilePath}: {ex.Message}");
        }
    }

    private void LoadModels()
    {
        try
        {
            if (File.Exists(_modelsFilePath))
            {
                var json = File.ReadAllText(_modelsFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _models = JsonSerializer.Deserialize<List<Model>>(json, options) ?? new List<Model>();
            }
            else
            {
                // Create a default models.json file if it doesn't exist
                CreateDefaultModelsFile();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load models from {_modelsFilePath}: {ex.Message}");
            _models = new List<Model>();
        }
    }

    private void CreateDefaultModelsFile()
    {
        try
        {
            var defaultModels = new List<Model>
            {
                new Model
                {
                    Name = "GPT-4 Turbo",
                    ShortName = "gpt-4-turbo",
                    Endpoint = "https://api.openai.com/v1",
                    Key = "sk-..."
                },
                new Model
                {
                    Name = "Claude 3.5 Sonnet",
                    ShortName = "claude-3-5-sonnet",
                    Endpoint = "https://api.anthropic.com/v1",
                    Key = "sk-ant-..."
                },
                new Model
                {
                    Name = "Llama 3 8B (Ollama)",
                    ShortName = "llama3-8b",
                    Endpoint = "http://localhost:11434/v1",
                    Key = "not-used-with-ollama"
                }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(defaultModels, options);
            Directory.CreateDirectory(Path.GetDirectoryName(_modelsFilePath)!);
            File.WriteAllText(_modelsFilePath, json);
            
            _models = defaultModels;
            Console.WriteLine($"Created default models file at {_modelsFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create default models file: {ex.Message}");
        }
    }

    /// <summary>Displays all configured models in a formatted table.</summary>
    public void DisplayModels()
    {
        if (Models == null || Models.Count == 0)
        {
            Console.WriteLine("No models found.");
            return;
        }

        // Calculate column widths for proper formatting
        var shortnameWidth = Math.Max("Shortname".Length, Models.Max(m => m.ShortName.Length));
        var nameWidth = Math.Max("Model".Length, Models.Max(m => m.Name.Length));
        var endpointWidth = Math.Max("Endpoint".Length, Models.Max(m => m.Endpoint.Length));

        // Ensure minimum widths for better readability
        shortnameWidth = Math.Max(shortnameWidth, 12);
        nameWidth = Math.Max(nameWidth, 15);
        endpointWidth = Math.Max(endpointWidth, 25);

        // Header
        Console.WriteLine($"{PadRight("Shortname", shortnameWidth)} | {PadRight("Model", nameWidth)} | {"Endpoint"}");
        Console.WriteLine($"{new string('-', shortnameWidth)} | {new string('-', nameWidth)} | {new string('-', endpointWidth)}");

        // Model data
        foreach (var model in Models)
        {
            Console.WriteLine($"{PadRight(model.ShortName, shortnameWidth)} | {PadRight(model.Name, nameWidth)} | {model.Endpoint}");
        }
    }

    private static string PadRight(string text, int width)
    {
        if (text.Length >= width)
            return text.Length > width ? text.Substring(0, width) : text;
        return text.PadRight(width);
    }
}