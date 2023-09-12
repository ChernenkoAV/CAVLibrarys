using System.Security.Cryptography.X509Certificates;

namespace Cav.DigitalSignature;

/// <summary>
/// Общие механизмы, не зависящие от провайдера криптографии
/// </summary>
public static class DSGeneric
{
    /// <summary>
    /// Получение сертификата по отпечатоку или из строки. (+ невалидные)
    /// </summary>
    /// <param name="thumbprintOrBase64Cert">Отперчаток или сертификат в BASE64</param>
    /// <param name="localMachine">Хранилище. null - смотреть везде, true - локальный компьютер, false - пользователь</param>
    /// <returns></returns>
    public static X509Certificate2? FindCertByThumbprint(string thumbprintOrBase64Cert, bool? localMachine = null)
    {
        if (thumbprintOrBase64Cert.IsNullOrWhiteSpace())
            return null;

        thumbprintOrBase64Cert = new string(thumbprintOrBase64Cert.ToCharArray().Where(x => char.IsLetterOrDigit(x) || x.In('+', '/', '=')).ToArray());

        X509Certificate2? cert = null;

        try
        {
            cert = new X509Certificate2(Convert.FromBase64String(thumbprintOrBase64Cert));
            return cert;
        }
        catch
        {
        }

        if (!localMachine.HasValue || localMachine.Value)
        {
            using var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var cc = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrBase64Cert, false);
            if (cc.Count != 0)
                cert = cc[0];
        }

        if (cert != null)
            return cert;

        if (!localMachine.HasValue || localMachine.Value == false)
        {
            using var store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var cc = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrBase64Cert, false);
            if (cc.Count != 0)
                cert = cc[0];
        }

        return cert;
    }
}
