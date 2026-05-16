using System.Configuration;
using System.Data;
using System.Windows;

using BlockFactory.Data;
using BlockFactory.Data.Seeders;
using BlockFactory.Desktop.Infrastructure;
using BlockFactory.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace BlockFactory.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            GlobalExceptionHandler.Register();

            Services = DependencyInjection.Configure();

            await InitializeDatabaseAsync();

            // تشغيل النسخ الاحتياطي التلقائي
            var backupService = GetService<BackupService>();
            backupService.Start();

            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        // الحصول على Service من أي مكان في التطبيق
        public static T GetService<T>() where T : notnull
            => Services.GetRequiredService<T>();
        private static async Task InitializeDatabaseAsync()
        {
            try
            {
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                await context.Database.MigrateAsync();
                await DatabaseSeeder.SeedAsync(context);
            }
            catch (Exception ex)
            {
                // ToString() يتضمن Inner + StackTrace — أدق من Message وحده لمشاكل EF/SqlClient
                MessageBox.Show(
                    ex.ToString(),
                    "خطأ فادح — التفاصيل الكاملة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Current.Shutdown();
            }
        }

        /*  private static async Task InitializeDatabaseAsync()
          {
              try
              {
                  using var scope = Services.CreateScope();
                  var context = scope.ServiceProvider
                      .GetRequiredService<AppDbContext>();

                  await context.Database.MigrateAsync();
                  await DatabaseSeeder.SeedAsync(context);
              }
              catch (Exception ex)
              {
                  MessageBox.Show(
                      $"خطأ في تهيئة قاعدة البيانات:\n{ex.Message}",
                      "خطأ فادح",
                      MessageBoxButton.OK,
                      MessageBoxImage.Error
                  );
                  Current.Shutdown();
              }
          }*/
    }
}
