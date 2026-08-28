namespace GerenciadorDeFinancasASPNET.Services {
    public interface IImportSessionService {
        ImportSession Save(ImportSession session);
        ImportSession? Load(string id);
        void Delete(string id);
    }
}
