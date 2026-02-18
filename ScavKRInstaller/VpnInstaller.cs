using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace ScavKRInstaller;

public static class VpnInstaller
{
    private const string OpenVpnMsiUrl = "https://swupdate.openvpn.org/community/releases/OpenVPN-2.6.15-I001-amd64.msi";
    private const string VpnServerAddress = "26.35.34.177";
    private const string VpnSharedPassword = "123";

    public static async Task<string> TryInstallOpenSourceVpnAsync()
    {
        try
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string openVpnExe = Path.Combine(programFiles, "OpenVPN", "bin", "openvpn.exe");
            if (File.Exists(openVpnExe))
            {
                return "OpenVPN Community is already installed.";
            }

            string workDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ScavKRInstaller");
            Directory.CreateDirectory(workDir);
            string msiPath = Path.Combine(workDir, "OpenVPN-2.6.15-I001-amd64.msi");

            using (HttpClient client = new())
            using (HttpResponseMessage response = await client.GetAsync(OpenVpnMsiUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using Stream download = await response.Content.ReadAsStreamAsync();
                await using FileStream fs = new(msiPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await download.CopyToAsync(fs);
            }

            ProcessStartInfo psi = new(
                "msiexec.exe",
                $"/i \"{msiPath}\" /qn /norestart"
            )
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? p = Process.Start(psi);
            if (p == null)
            {
                return "Failed to start OpenVPN installer process.";
            }

            await p.WaitForExitAsync();
            if (p.ExitCode == 0 || p.ExitCode == 3010)
            {
                return p.ExitCode == 0
                    ? "OpenVPN Community installed successfully."
                    : "OpenVPN Community installed successfully (reboot required by installer).";
            }

            return $"OpenVPN installer exited with code {p.ExitCode}.";
        }
        catch (Exception ex)
        {
            return $"OpenVPN install failed: {ex.Message}";
        }
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
                "Client VPN: OpenVPN Community (installed by this installer)\r\n" +
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
