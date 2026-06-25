using NexusCode.Roslyn;

namespace NexusCode.Tests.Fixtures;

public static class SampleRepository
{
    public static string CreateTempRepo()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NexusTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);

        CreateProjectFile(srcDir, "TestProject.csproj");

        CreateFile(srcDir, "PlayerController.cs", @"
using System;
namespace TestGame
{
    public class PlayerController
    {
        public int Health { get; set; }
        public Weapon CurrentWeapon { get; set; }
        
        public void Attack()
        {
            CurrentWeapon.Fire();
        }
        
        public void TakeDamage(int amount)
        {
            Health -= amount;
        }
    }
}");

        CreateFile(srcDir, "Weapon.cs", @"
using System;
namespace TestGame
{
    public class Weapon
    {
        public int Damage { get; set; }
        public Projectile ProjectilePrefab { get; set; }
        
        public void Fire()
        {
            var proj = new Projectile();
            proj.Spawn();
        }
    }
}");

        CreateFile(srcDir, "Projectile.cs", @"
using System;
namespace TestGame
{
    public class Projectile
    {
        public float Speed { get; set; }
        
        public void Spawn()
        {
            Console.WriteLine(""Projectile spawned"");
        }
    }
}");

        CreateFile(srcDir, "IDamageable.cs", @"
namespace TestGame
{
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}");

        CreateFile(srcDir, "Enemy.cs", @"
using System;
namespace TestGame
{
    public class Enemy : IDamageable
    {
        public int Health { get; set; }
        
        public void TakeDamage(int amount)
        {
            Health -= amount;
        }
    }
}");

        return tempDir;
    }

    public static void Cleanup(string repoPath)
    {
        try { Directory.Delete(repoPath, true); } catch { }
    }

    private static void CreateProjectFile(string dir, string name)
    {
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
        File.WriteAllText(Path.Combine(dir, name), content);
    }

    private static void CreateFile(string dir, string name, string content)
    {
        File.WriteAllText(Path.Combine(dir, name), content);
    }
}
