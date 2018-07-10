using dm.Banotto.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dm.Banotto.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(AppDbContext db)
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (var tx = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        //if (!db.Rounds.Any())
                        //{
                        //    var items = new Round[]
                        //    {
                        //        new Round {
                        //            Created = DateTime.Now,
                        //            Start = DateTime.Now,
                        //            End = DateTime.Now.AddDays(30)
                        //        }
                        //    };
                        //    db.Rounds.AddRange(items);
                        //    await db.SaveChangesAsync();
                        //}

                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                    }
                }
            });
        }
    }
}