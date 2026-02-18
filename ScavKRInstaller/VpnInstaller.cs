using System.IO;

namespace ScavKRInstaller;

public static class VpnInstaller
{
    private const string VpnServerAddress = "26.35.34.177";
    private const string VpnSharedPassword = "123";

    public static Task<string> TryInstallOpenSourceVpnAsync()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string openVpnExe = Path.Combine(programFiles, "OpenVPN", "bin", "openvpn.exe");
        if (File.Exists(openVpnExe))
        {
            return Task.FromResult("OpenVPN already detected. No-admin mode: keeping existing installation.");
        }

        return Task.FromResult("No-admin mode active: skipped VPN system installation. Game install continues without admin rights.");
    }

    public static string WriteGuestVpnInfoFile(string gameFolderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath))
            {
                return "VPN info file skipped: game folder path is empty.";
            }

            string infoPath = Path.Combine(gameFolderPath, "VPN_GUEST_INFO.txt");
            string info =
                "VPN QUICK INFO (guest)\r\n" +
                "======================\r\n" +
                "Client VPN: use an already installed VPN client (no-admin installer mode)\r\n" +
                $"VPN server/address: {VpnServerAddress}\r\n" +
                $"Shared password: {VpnSharedPassword}\r\n\r\n" +
                "Note: OpenVPN does not auto-connect with only IP/password.\r\n" +
                "You still need the server profile (.ovpn) provided by the host.\r\n";

            File.WriteAllText(infoPath, info);
            return $"Created VPN guest info file: {infoPath}";
        }
        catch (Exception ex)
        {
            return $"Failed to write VPN guest info file: {ex.Message}";
        }
    }

    public static string EnsureGuestVpnRuntimeFiles(string gameFolderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath))
            {
                return "VPN runtime files skipped: game folder path is empty.";
            }

            string vpnDir = Path.Combine(gameFolderPath, "vpn");
            Directory.CreateDirectory(vpnDir);

            string credentialsPath = Path.Combine(vpnDir, "credentials.txt");
            File.WriteAllText(credentialsPath, "guest\r\n123\r\n");

            string templatePath = Path.Combine(vpnDir, "guest.ovpn.template");
            string templateContent =
                "client\r\n" +
                "dev tun\r\n" +
                "proto udp\r\n" +
                $"remote {VpnServerAddress} 1194\r\n" +
                "resolv-retry infinite\r\n" +
                "nobind\r\n" +
                "persist-key\r\n" +
                "persist-tun\r\n" +
                "auth-user-pass credentials.txt\r\n" +
                "remote-cert-tls server\r\n" +
                "verb 3\r\n\r\n" +
                "# Host must provide a complete guest.ovpn profile with certificates.\r\n" +
                "# Copy/rename this file to guest.ovpn and add required CA/cert/key blocks if needed.\r\n";
            File.WriteAllText(templatePath, templateContent);

            return $"Prepared VPN runtime files in: {vpnDir}";
        }
        catch (Exception ex)
        {
            return $"Failed to prepare VPN runtime files: {ex.Message}";
        }
    }
}
