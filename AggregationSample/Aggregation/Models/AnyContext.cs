using System.Data.Entity;

namespace AutoExpand.Models
{
    public class AnyContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, please use data migrations.
        // For more information refer to the documentation:
        // http://msdn.microsoft.com/en-us/data/jj591621.aspx

        public AnyContext() : base("name=AnyContext")
        {
            Database.SetInitializer(new AnyDbInitializer());
        }

        public DbSet<Contribution> Contributions { get; set; }
        public DbSet<Contact> Contacts { get; set; }
    }

    public class AnyDbInitializer : DropCreateDatabaseAlways<AnyContext>
    {
        protected override void Seed(AnyContext context)
        {
            var tableDef = new Contact
            {
                ContactID = 1,
                FirstName = "Test",
                LastName = "Test",
            };
            var contributions = new[] { new Contribution() { ContributionID = 1, Amount = 1, Contact = tableDef} };
            context.Contributions.AddRange(contributions);
            tableDef.Contributions = contributions;
            context.Contacts.Add(tableDef);
        }
    }
}