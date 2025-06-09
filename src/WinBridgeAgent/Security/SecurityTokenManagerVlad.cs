// Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WinBridgeAgent.Core;

namespace WinBridgeAgent.Security
{
    // Part of WinBridgeAgent (c) 2025 Vladislav Manoilov
    public interface ITokenManager
    {
        string GenerateToken();
        bool ValidateToken(string token);
        void CleanupExpiredTokens(); // Optional: For active cleanup if not using self-expiring tokens like JWT
    }

    public class SecurityTokenManagerVlad : ITokenManager, IDisposable
    {
        private readonly ILogger<SecurityTokenManagerVlad> _logger;
        private readonly SecuritySettings _securitySettings;
        private readonly ConcurrentDictionary<string, DateTime> _activeTokens = new ConcurrentDictionary<string, DateTime>();
        private readonly Timer _cleanupTimer;

        public SecurityTokenManagerVlad(ILogger<SecurityTokenManagerVlad> logger, IOptions<SecuritySettings> securitySettings)
        {
            _logger = logger;
            _securitySettings = securitySettings.Value;
            _logger.LogInformation("SecurityTokenManagerVlad initialized. Token Validity: {TokenValidityMinutes} minutes.", _securitySettings.TokenValidityMinutes);
            // Cleanup expired tokens periodically (e.g., every 5 minutes)
            _cleanupTimer = new Timer(e => CleanupExpiredTokens(), null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        public string GenerateToken()
        {
            // Using a simple cryptographically secure random string for the token
            // For production, consider JWT or a more robust token scheme if external validation or claims are needed.
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            string token = Convert.ToBase64String(randomNumber);
            
            DateTime expiration = DateTime.UtcNow.AddMinutes(_securitySettings.TokenValidityMinutes);
            _activeTokens[token] = expiration;

            _logger.LogInformation("Generated new token. Expires at: {Expiration}", expiration);
            // Log.WinBridge.CreateAudit for token generation (if required by audit policy)
            _logger.LogInformation("Log.WinBridge.CreateAudit: New token generated."); 
            return token;
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token validation failed: Token is null or empty.");
                return false;
            }

            if (_activeTokens.TryGetValue(token, out DateTime expirationTime))
            {
                if (expirationTime >= DateTime.UtcNow)
                {
                    _logger.LogInformation("Token validated successfully.");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Token validation failed: Token expired at {ExpirationTime}", expirationTime);
                    _activeTokens.TryRemove(token, out _); // Remove expired token
                    return false;
                }
            }

            _logger.LogWarning("Token validation failed: Token not found or invalid.");
            return false;
        }

        public void CleanupExpiredTokens()
        {
            int removedCount = 0;
            foreach (var tokenPair in _activeTokens.ToList()) // ToList() to avoid modification during enumeration
            {
                if (tokenPair.Value < DateTime.UtcNow)
                {
                    if (_activeTokens.TryRemove(tokenPair.Key, out _))
                    {
                        removedCount++;
                    }
                }
            }
            if (removedCount > 0)
            {
                _logger.LogInformation("Cleaned up {RemovedCount} expired tokens.", removedCount);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}

