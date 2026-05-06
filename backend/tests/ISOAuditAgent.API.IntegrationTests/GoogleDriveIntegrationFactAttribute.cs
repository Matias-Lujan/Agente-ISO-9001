namespace ISOAuditAgent.API.IntegrationTests;

/// <summary>
/// Marca un test que requiere credenciales de service account de Google
/// y una carpeta de prueba real. Se omite automáticamente si las
/// variables de entorno requeridas no están definidas.
/// </summary>
/// <remarks>
/// Variables esperadas (configurar UNA de credenciales y la del folder):
/// <list type="bullet">
///   <item><description>
///     <c>GOOGLE_DRIVE_TEST_CREDENTIALS_PATH</c> — path al JSON de la
///     service account.
///   </description></item>
///   <item><description>
///     <c>GOOGLE_DRIVE_TEST_CREDENTIALS_JSON</c> — alternativa: contenido
///     inline del JSON.
///   </description></item>
///   <item><description>
///     <c>GOOGLE_DRIVE_TEST_FOLDER_ID</c> — FolderId raíz compartido con
///     la service account.
///   </description></item>
/// </list>
/// </remarks>
public sealed class GoogleDriveIntegrationFactAttribute : FactAttribute
{
    public const string CredentialsPathEnv = "GOOGLE_DRIVE_TEST_CREDENTIALS_PATH";
    public const string CredentialsJsonEnv = "GOOGLE_DRIVE_TEST_CREDENTIALS_JSON";
    public const string FolderIdEnv        = "GOOGLE_DRIVE_TEST_FOLDER_ID";

    public GoogleDriveIntegrationFactAttribute()
    {
        var hasCredsPath = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(CredentialsPathEnv));
        var hasCredsJson = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(CredentialsJsonEnv));
        var hasFolder = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(FolderIdEnv));

        if ((!hasCredsPath && !hasCredsJson) || !hasFolder)
        {
            Skip =
                $"Test de integración omitido. Para ejecutarlo definir " +
                $"{FolderIdEnv} y una de [{CredentialsPathEnv} | {CredentialsJsonEnv}].";
        }
    }
}
