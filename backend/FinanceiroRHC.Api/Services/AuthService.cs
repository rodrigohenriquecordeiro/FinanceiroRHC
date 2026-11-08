using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceiroRHC.Api.Data;
using FinanceiroRHC.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace FinanceiroRHC.Api.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name.Trim(),
            CompanyName = request.CompanyName?.Trim(),
            BenefitName = request.BenefitName.Trim()
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        var cardNames = request.CreditCards.Select(name => name.Trim()).Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var name in cardNames)
        {
            context.CreditCards.Add(new CreditCard { UserId = user.Id, Name = name });
        }
        foreach (var name in new[] { "Conta fixa", "Conta variável", "Alimentação", "Transporte", "Saúde", "Lazer" })
        {
            context.Categories.Add(new FinancialCategory { UserId = user.Id, Name = name, Type = "expense" });
        }
        await context.SaveChangesAsync();
        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }
        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(ApplicationUser user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configure Jwt:Key com User Secrets ou variável de ambiente.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key deve possuir pelo menos 32 bytes.");
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email!)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Name, user.Email!);
    }
}
