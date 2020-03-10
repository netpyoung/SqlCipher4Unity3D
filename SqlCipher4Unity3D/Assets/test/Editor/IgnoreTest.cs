using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.TestTools;
using IgnoreAttribute = SQLite.Attribute.IgnoreAttribute;
using SqlCipher4Unity3D;
using System.IO;

namespace Test
{
    public class Company
    {
        public string ID { get; set; }
    }

    public class Account
    {
        public string ID { get; set; }
        public string CompanyID { get; set; }
        [Ignore]
        public Company Company
        {
            get
            {
                return new Company();
            }
            set
            {
                CompanyID = value.ID;
            }
        }

        public override bool Equals(object obj)
        {
            Account o = obj as Account;
            if (o is null)
            {
                return false;
            }

            if (this.ID != o.ID)
            {
                return false;
            }

            if (this.CompanyID != o.CompanyID)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = 480581749;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.ID);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.CompanyID);
            hashCode = hashCode * -1521134295 + EqualityComparer<Company>.Default.GetHashCode(this.Company);
            return hashCode;
        }
    }

    [TestFixture]
    public class IgnoreTest
    {
        string DbPath = $"Assets/StreamingAssets/{nameof(IgnoreTest)}.db";
        SQLiteConnection Conn = null;
        [SetUp]
        public void Up()
        {
            Cleanup();
            this.Conn = new SQLiteConnection(DbPath, "");
        }

        [TearDown]
        public void Down()
        {
            if (Conn != null)
            {
                Conn.Close();
            }

            Cleanup();
        }

        void Cleanup()
        {
            if (File.Exists(DbPath))
            {
                File.Delete(DbPath);
            }
        }

        [Test]
        public void IgnoreTestSimplePasses()
        {
            Conn.CreateTable<Account>();

            Account a = new Account { ID = "A", CompanyID = "X" };
            Conn.Insert(a);
            Account b = Conn.Table<Account>().Where(x => x.ID == "A").First();
            Assert.AreEqual(a, b);
        }
    }
}
