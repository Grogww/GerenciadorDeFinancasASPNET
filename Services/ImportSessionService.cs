using System.Text.Json;

namespace GerenciadorDeFinancasASPNET.Services {
    /// <summary>
    /// Persiste as importações pendentes como JSON em TempData/imports/{id}.json.
    /// Arquivo em disco (e não Session/memória) para o preview sobreviver a
    /// restarts do app e não exigir configuração de session state.
    /// </summary>
    public class ImportSessionService : IImportSessionService {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
        private readonly string _dir;

        public ImportSessionService(IWebHostEnvironment env) {
            _dir = Path.Combine(env.ContentRootPath, "TempData", "imports");
            Directory.CreateDirectory(_dir);
        }

        public ImportSession Save(ImportSession session) {
            File.WriteAllText(PathFor(session.Id), JsonSerializer.Serialize(session, JsonOptions));
            return session;
        }

        public ImportSession? Load(string id) {
            // O id vem da URL: valida o formato antes de usá-lo como nome de arquivo.
            if (!Guid.TryParseExact(id, "N", out _)) return null;

            var path = PathFor(id);
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<ImportSession>(File.ReadAllText(path));
        }

        public void Delete(string id) {
            if (!Guid.TryParseExact(id, "N", out _)) return;
            var path = PathFor(id);
            if (File.Exists(path)) File.Delete(path);
        }

        private string PathFor(string id) => Path.Combine(_dir, $"{id}.json");
    }
}
