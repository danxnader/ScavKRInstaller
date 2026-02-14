using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace ScavKRInstaller
{
    public static class VersionManager
    {
        public static SetupVersions Instance { get; private set; }
        public async static Task Init(string source)
        {
            Instance=await ParseVersion(source);
        }
        public async static Task<SetupVersions> ParseVersion(string source)
        {
            if(source != null)
            {
                LogHandler.Instance.Write($"Trying to initialize setup version from source: {source}");
                bool IsURL = Uri.IsWellFormedUriString(source, UriKind.RelativeOrAbsolute);
                if(IsURL)
                {
                    try
                    {
                        return await Fetch(source);
                    }
                    catch(Exception ex)
                    {
                        LogHandler.Instance.Write($"Couldn't fetch from {source}!");
                        goto localFallback;
                    }
                }
                else
                {
                    try
                    {
                        return LoadLocal(source);
                    }
                    catch(Exception ex)
                    {
                        LogHandler.Instance.Write($"Couldn't load local from {source}!");
                        goto localFallback;
                    }
                }
            }
            else
            {
                LogHandler.Instance.Write($"Got setup init with null, using local fallback");
            }
        localFallback:
            {
                if(Constants.FallbackVersionFilePath != null)
                {
                    return LoadLocal(Constants.FallbackVersionFilePath);
                }
                LogHandler.Instance.Write($"Version init FAILURE! No local sources file found!");
                throw new FileNotFoundException("No fallback sources file found!");
            }
        }
        public async static Task<SetupVersions> Fetch(string source)
        {
            LogHandler.Instance.Write($"Trying to download sources file from: {source}");
            using HttpClient client = new();
            Uri uri = new(source);
            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                return Deserialize(source);
            }
            catch(TaskCanceledException ex) when(ex.InnerException is TimeoutException)
            {
                LogHandler.Instance.Write($"!!TIMEOUT WHILE GETTING SOURCE {source}!!");
                throw new TimeoutException();
            }
            catch(HttpRequestException ex)
            {
                LogHandler.Instance.Write($"!!SERVER UNREACHABLE WHILE GETTING SOURCE {source}!!");
                throw new TimeoutException();
            }
        }
        public static SetupVersions LoadLocal(string source)
        {
            string json = File.ReadAllText(source);
            return Deserialize(json);
        }

        private static SetupVersions Deserialize(string source)
        {
            if(source==null)
            {
                LogHandler.Instance.Write($"Got null string while deserializing!");
                throw new ArgumentNullException("Got null JSON string while deserializing versions!");
            }
            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive=true };
            SetupVersionsDTO versions = JsonSerializer.Deserialize<SetupVersionsDTO>(source, options);
            if(versions==null)
            {
                LogHandler.Instance.Write($"Got null JSON while deserializing!");
                throw new ArgumentNullException("Couldn't deserialize JSON!");
            }
            SetupVersions result = new();
            result.FromDTO(versions);
            return result;
        }
    }
}
