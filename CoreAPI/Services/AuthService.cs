using CoreAPI.DataBaseContext;
using CoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CoreAPI.Services
{
    public class AuthService
    {
        private readonly APIDBContext _context;
        private readonly JwtService _jwtService;

        public AuthService(APIDBContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResponse> RegisterAsync(UserRegistrationRequest request)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = passwordHash,
                Company = request.Company,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

            // Save refresh token
            var refreshTokenEntity = new DataBaseContext.RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                ExpiresAt = refreshTokenExpiry
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserResponse
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Company = user.Company,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        }

        public async Task<AuthResponse> LoginAsync(UserLoginRequest request)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                throw new InvalidOperationException("Invalid email or password");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new InvalidOperationException("Invalid email or password");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

            // Revoke existing refresh tokens and save new one
            //var existingTokens = await _context.RefreshTokens
            //    .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
            //    .ToListAsync();

            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsRevoked = true;
            }

            var refreshTokenEntity = new DataBaseContext.RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                ExpiresAt = refreshTokenExpiry
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserResponse
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Company = user.Company,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            // Find the refresh token
            var tokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (tokenEntity == null || tokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Invalid refresh token");
            }

            // Generate new tokens
            var accessToken = _jwtService.GenerateAccessToken(tokenEntity.User);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

            // Revoke old token and save new one
            tokenEntity.IsRevoked = true;

            var newRefreshTokenEntity = new DataBaseContext.RefreshToken
            {
                UserId = tokenEntity.UserId,
                Token = newRefreshToken,
                ExpiresAt = refreshTokenExpiry
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                User = new UserResponse
                {
                    UserId = tokenEntity.User.UserId,
                    FirstName = tokenEntity.User.FirstName,
                    LastName = tokenEntity.User.LastName,
                    Email = tokenEntity.User.Email,
                    Company = tokenEntity.User.Company,
                    Role = tokenEntity.User.Role,
                    CreatedAt = tokenEntity.User.CreatedAt,
                    LastLoginAt = tokenEntity.User.LastLoginAt
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenEntity != null)
            {
                tokenEntity.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
