namespace Chance.Application.Infrastructure.Database_Tests
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Cysharp.Threading.Tasks;
	using global::SqlCipher4Unity3D;
	using NUnit.Framework;
	using SqlCipher4Unity3D;
	using SQLite.Attributes;
	using UnityEngine;
	using UnityEngine.TestTools;

	[Serializable]
	public class Customer
	{
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }

		[MaxLength (64)]
		public string FirstName { get; set; }

		[MaxLength (64)]
		public string LastName { get; set; }

		[MaxLength (64), Indexed]
		public string Email { get; set; }
	}

	/// <summary>
	/// Defines tests that exercise async behaviour.
	/// </summary>
	[TestFixture]
	public class AsyncTests
	{
		private const string DatabaseName = "async.db";

		[UnityTest]
		public IEnumerator EnableWalAsync () => UniTask.ToCoroutine(async () => 
		{
			var path = TestPath.GetTempFileName ();
			var connection = new SQLiteAsyncConnection (path);

			await connection.EnableWriteAheadLoggingAsync ();
		});

		[UnityTest]
		public IEnumerator QueryAsync () => UniTask.ToCoroutine(async () => 
		{
			var connection = GetConnection ();
			await connection.CreateTableAsync<Customer> ();

			var customer = new Customer {
				FirstName = "Joe"
			};

			await connection.InsertAsync (customer);

			await connection.QueryAsync<Customer> ("select * from Customer");
		});

		[UnityTest]
		public IEnumerator MemoryQueryAsync () => UniTask.ToCoroutine(async () => 
		{
			var connection = new SQLiteAsyncConnection (":memory:", false);
			await connection.CreateTableAsync<Customer> ();

			var customer = new Customer {
				FirstName = "Joe"
			};

			await connection.InsertAsync (customer);

			await connection.QueryAsync<Customer> ("select * from Customer");
		});

		[UnityTest]
		public IEnumerator StressAsync () => UniTask.ToCoroutine(async () => 
		{
			string path = null;
			var globalConn = GetConnection (ref path);
			
			await globalConn.CreateTableAsync<Customer> ();
			
			var n = 100;
			var errors = new List<string> ();
			for (var i = 0; i < n; i++) {
				var ii = i;
				try {
					var conn = GetConnection ();
					var obj = new Customer {
						FirstName = ii.ToString (),
					};
					await conn.InsertAsync (obj);
					if (obj.Id == 0) {
						lock (errors) {
							errors.Add ("Bad Id");
						}
					}
					var obj2 = (await (from c in conn.Table<Customer> () where c.Id == obj.Id select c).ToListAsync()).FirstOrDefault();
					if (obj2 == null) {
						lock (errors) {
							errors.Add ("Failed query");
						}
					}
				}
				catch (Exception ex) {
					lock (errors) {
						errors.Add (ex.Message);
					}
				}
                
			}

			var count = await globalConn.Table<Customer>().CountAsync();

			foreach (var e in errors) {
				Console.WriteLine ("ERROR " + e);
			}
			
			Assert.AreEqual (0, errors.Count);
			Assert.AreEqual (n, count);			
		});

		[UnityTest]
		public IEnumerator TestCreateTableAsync () => UniTask.ToCoroutine(async () => 
		{
			string path = null;
			var conn = GetConnection (ref path);

			// drop the customer table...
			await conn.ExecuteAsync("drop table if exists Customer");

			// run...
			await conn.CreateTableAsync<Customer>();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				// run it - if it's missing we'll get a failure...
				check.Execute ("select * from Customer");
			}
		});

		SQLiteAsyncConnection GetConnection ()
		{
			string path = null;
			return GetConnection (ref path);
		}
		
		string _path;
		string _connectionString;
		
		[SetUp]
		public void SetUp()
		{
			_connectionString = TestPath.GetTempFileName();
			_path = _connectionString;
			System.IO.File.Delete (_path);
		}
		
		SQLiteAsyncConnection GetConnection (ref string path)
		{
			path = _path;
			return new SQLiteAsyncConnection (_connectionString);
		}

		[UnityTest]
		public IEnumerator TestDropTableAsync () => UniTask.ToCoroutine(async () => 
		{
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();

			// drop it...
			await conn.DropTableAsync<Customer> ();

			// check...
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				// load it back and check - should be missing
				var command = check.CreateCommand ("select name from sqlite_master where type='table' and name='customer'");
				Assert.IsNull (command.ExecuteScalar<string> ());
			}
		});

		private Customer CreateCustomer ()
		{
			Customer customer = new Customer () {
				FirstName = "foo",
				LastName = "bar",
				Email = Guid.NewGuid ().ToString ()
			};
			return customer;
		}

		[UnityTest]
		public IEnumerator TestInsertAsync () => UniTask.ToCoroutine(async () => 
		{
			// create...
			Customer customer = this.CreateCustomer ();

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();

			// run...
			await conn.InsertAsync (customer);

			// check that we got an id...
			Assert.AreNotEqual (0, customer.Id);

			// check...
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				// load it back...
				Customer loaded = check.Get<Customer> (customer.Id);
				Assert.AreEqual (loaded.Id, customer.Id);
			}
		});

		[UnityTest]
		public IEnumerator TestUpdateAsync () => UniTask.ToCoroutine(async () => 
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();

			// run...
			await conn.InsertAsync (customer);

			// change it...
			string newEmail = Guid.NewGuid ().ToString ();
			customer.Email = newEmail;

			// save it...
			await conn.UpdateAsync (customer);

			// check...
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				// load it back - should be changed...
				Customer loaded = check.Get<Customer> (customer.Id);
				Assert.AreEqual (newEmail, loaded.Email);
			}
		});

		[UnityTest]
		public IEnumerator TestDeleteAsync () => UniTask.ToCoroutine(async () => 
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();

			// run...
			await conn.InsertAsync (customer);

			// delete it...
			await conn.DeleteAsync (customer);

			// check...
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				// load it back - should be null...
				var loaded = check.Table<Customer> ().Where (v => v.Id == customer.Id).ToList ();
				Assert.AreEqual (0, loaded.Count);
			}
		});

		[UnityTest]
		public IEnumerator GetAsync () => UniTask.ToCoroutine(async () => 
		{
			// create...
			Customer customer = new Customer ();
			customer.FirstName = "foo";
			customer.LastName = "bar";
			customer.Email = Guid.NewGuid ().ToString ();

			// connect and insert...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();
			await conn.InsertAsync (customer);

			// check...
			Assert.AreNotEqual (0, customer.Id);

			// get it back...
			var task = conn.GetAsync<Customer> (customer.Id);
			
			Customer loaded = await task;

			// check...
			Assert.AreEqual (customer.Id, loaded.Id);
		});
		
		[UnityTest]
		public IEnumerator FindAsyncWithExpression () => UniTask.ToCoroutine(async () => 
		{
			// create...
			Customer customer = new Customer ();
			customer.FirstName = "foo";
			customer.LastName = "bar";
			customer.Email = Guid.NewGuid ().ToString ();

			// connect and insert...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();
			await conn.InsertAsync (customer);

			// check...
			Assert.AreNotEqual (0, customer.Id);

			// get it back...
			var task = conn.FindAsync<Customer> (x => x.Id == customer.Id);
			
			Customer loaded = await task;

			// check...
			Assert.AreEqual (customer.Id, loaded.Id);
		});
		
		[UnityTest]
		public IEnumerator FindAsyncWithExpressionNull () => UniTask.ToCoroutine(async () => 
		{
			// connect and insert...
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer> ();

			// get it back...
			var task = conn.FindAsync<Customer> (x => x.Id == 1);
			
			var loaded = await task;

			// check...
			Assert.IsNull (loaded);
		});

		[UnityTest]
		public IEnumerator TestFindAsyncItemPresent () => UniTask.ToCoroutine(async () => 
		{
			// create...
			Customer customer = CreateCustomer ();

			// connect and insert...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();
			await conn.InsertAsync (customer);

			// check...
			Assert.AreNotEqual (0, customer.Id);

			// get it back...
			var task = conn.FindAsync<Customer>(customer.Id);
			Customer loaded = await task;

			// check...
			Assert.AreEqual (customer.Id, loaded.Id);
		});

		[UnityTest]
		public IEnumerator TestFindAsyncItemMissing () => UniTask.ToCoroutine(async () => 
		{
			// connect and insert...
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer> ();

			// now get one that doesn't exist...
			var task = conn.FindAsync<Customer> (-1);

			// check...
			Assert.IsNull (await task);
		});

		[UnityTest]
		public IEnumerator TestQueryAsync () => UniTask.ToCoroutine(async () => 
		{
			// connect...
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer> ();

			// insert some...
			List<Customer> customers = new List<Customer> ();
			for (int index = 0; index < 5; index++) {
				Customer customer = CreateCustomer ();

				// insert...
				await conn.InsertAsync (customer);

				// add...
				customers.Add (customer);
			}

			// return the third one...
			var task = conn.QueryAsync<Customer> ("select * from customer where id=?", customers[2].Id);
			var loaded = await task;

			// check...
			Assert.AreEqual (1, loaded.Count);
			Assert.AreEqual (customers[2].Email, loaded[0].Email);
		});

		[UnityTest]
		public IEnumerator TestTableAsync () => UniTask.ToCoroutine(async () => 
		{
			// connect...
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer> ();
			await conn.ExecuteAsync ("delete from customer");

			// insert some...
			List<Customer> customers = new List<Customer> ();
			for (int index = 0; index < 5; index++) {
				Customer customer = new Customer ();
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid ().ToString ();

				// insert...
				await conn.InsertAsync (customer);

				// add...
				customers.Add (customer);
			}

			// run the table operation...
			var query = conn.Table<Customer> ();
			var loaded = await query.ToListAsync ();

			// check that we got them all back...
			Assert.AreEqual (5, loaded.Count);
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[0].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[1].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[2].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[3].Id));
			Assert.IsNotNull (loaded.Where (v => v.Id == customers[4].Id));
		});

		[UnityTest]
		public IEnumerator TestExecuteAsync () => UniTask.ToCoroutine(async () => 
		{
			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();

			// do a manual insert...
			string email = Guid.NewGuid ().ToString ();
			await conn.ExecuteAsync ($"insert into customer (firstname, lastname, email) values (?, ?, ?)",
				"foo", "bar", email);

			// check...
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				// load it back - should be null...
				var result = check.Table<Customer> ().Where (v => v.Email == email);
				Assert.IsNotNull (result);
			}
		});

		[UnityTest]
		public IEnumerator TestInsertAllAsync () => UniTask.ToCoroutine(async () => 
		{
			// create a bunch of customers...
			List<Customer> customers = new List<Customer> ();
			for (int index = 0; index < 100; index++) {
				Customer customer = new Customer ();
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid ().ToString ();
				customers.Add (customer);
			}

			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();

			// insert them all...
			await conn.InsertAllAsync (customers);

			// check...
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				for (int index = 0; index < customers.Count; index++) {
					// load it back and check...
					Customer loaded = check.Get<Customer> (customers[index].Id);
					Assert.AreEqual (loaded.Email, customers[index].Email);
				}
			}
		});

		[UnityTest]
		public IEnumerator TestRunInTransactionAsync () => UniTask.ToCoroutine(async () => 
		{
			// connect...
			string path = null;
			var conn = GetConnection (ref path);
			await conn.CreateTableAsync<Customer> ();
			bool transactionCompleted = false;

			// run...
			Customer customer = new Customer ();
			await conn.RunInTransactionAsync((c) =>
			{
				// insert...
				customer.FirstName = "foo";
				customer.LastName = "bar";
				customer.Email = Guid.NewGuid().ToString();
				c.Insert(customer);

				// delete it again...
				c.Execute("delete from customer where id=?", customer.Id);

				// set completion flag
				transactionCompleted = true;
			});

			// check...
			Assert.IsTrue(transactionCompleted);
			using (SQLiteConnection check = new SQLiteConnection (new SQLiteConnectionString(path))) {
				// load it back and check - should be deleted...
				var loaded = check.Table<Customer> ().Where (v => v.Id == customer.Id).ToList ();
				Assert.AreEqual (0, loaded.Count);
			}
		});

		[UnityTest]
		public IEnumerator TestExecuteScalar () => UniTask.ToCoroutine(async () => 
		{
			// connect...
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer> ();

			// check...
			var task = conn.ExecuteScalarAsync<object> ("select name from sqlite_master where type='table' and name='customer'");
			object name = await task;
			Assert.AreNotEqual ("Customer", name);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryToListAsync () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer> ();

			// create...
			Customer customer = this.CreateCustomer ();
			await conn.InsertAsync (customer);

			// query...
			var query = conn.Table<Customer> ();
			var task = query.ToListAsync ();
			var items = await task;

			// check...
			var loaded = items.Where (v => v.Id == customer.Id).First ();
			Assert.AreEqual (customer.Email, loaded.Email);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryToFirstAsyncFound () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();

			// create...
			Customer customer = this.CreateCustomer();
			await conn.InsertAsync(customer);

			// query...
			var query = conn.Table<Customer> ().Where(v => v.Id == customer.Id);
			var task = query.FirstAsync ();
			
			var loaded = await task;

			// check...
			Assert.AreEqual(customer.Email, loaded.Email);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryToFirstAsyncMissing () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection();
			await conn.CreateTableAsync<Customer>();

			// create...
			Customer customer = this.CreateCustomer();
			await conn.InsertAsync(customer);

			// query...
			var query = conn.Table<Customer>().Where(v => v.Id == -1);
			try
			{
				var task = await query.FirstAsync();
				
				//if Exception not occurs assert
				Assert.Fail("InvalidOperationException Exception expected");
			}
			catch (Exception e)
			{
				Assert.IsTrue(e is InvalidOperationException);
			}
			// can't use ExceptionAssert with async function
			//ExceptionAssert.Throws<AggregateException>(async () => await query.FirstAsync());
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryToFirstOrDefaultAsyncFound () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection();
			await conn.CreateTableAsync<Customer>();

			// create...
			Customer customer = this.CreateCustomer();
			await conn.InsertAsync(customer);

			// query...
			var query = conn.Table<Customer>().Where(v => v.Id == customer.Id);
			var task = query.FirstOrDefaultAsync();
			var loaded = await task;

			// check...
			Assert.AreEqual(customer.Email, loaded.Email);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryToFirstOrDefaultAsyncMissing () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection();
			await conn.CreateTableAsync<Customer>();

			// create...
			Customer customer = this.CreateCustomer();
			await conn.InsertAsync(customer);

			// query...
			var query = conn.Table<Customer>().Where(v => v.Id == -1);
			var task = query.FirstOrDefaultAsync();
			var loaded = await task;

			// check...
			Assert.IsNull(loaded);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryWhereOperation () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();

			// create...
			Customer customer = this.CreateCustomer ();
			await conn.InsertAsync(customer);

			// query...
			var query = conn.Table<Customer> ();
			var task = query.ToListAsync ();
			var items = await task;

			// check...
			var loaded = items.Where (v => v.Id == customer.Id).First ();
			Assert.AreEqual (customer.Email, loaded.Email);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryCountAsync () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();
			await conn.ExecuteAsync("delete from customer");

			// create...
			for (int index = 0; index < 10; index++)
				await conn.InsertAsync(this.CreateCustomer());

			// load...
			var query = conn.Table<Customer> ();
			var task = query.CountAsync ();

			// check...
			Assert.AreEqual (10, await task);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableOrderBy () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();
			await conn.ExecuteAsync("delete from customer");

			// create...
			for (int index = 0; index < 10; index++)
				await conn.InsertAsync(this.CreateCustomer());

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.Email);
			var task = query.ToListAsync ();
			var items = await task;

			// check...
			Assert.AreEqual (-1, string.Compare (items[0].Email, items[9].Email));
		});

		[UnityTest]
		public IEnumerator TestAsyncTableOrderByDescending () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();
			await conn.ExecuteAsync("delete from customer");

			// create...
			for (int index = 0; index < 10; index++)
				await conn.InsertAsync(this.CreateCustomer());

			// query...
			var query = conn.Table<Customer> ().OrderByDescending (v => v.Email);
			var task = query.ToListAsync ();
			var items = await task;

			// check...
			Assert.AreEqual (1, string.Compare (items[0].Email, items[9].Email));
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQueryTake () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();
			await conn.ExecuteAsync("delete from customer");

			// create...
			for (int index = 0; index < 10; index++) {
				var customer = this.CreateCustomer ();
				customer.FirstName = index.ToString ();
				await conn.InsertAsync(customer);
			}

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.FirstName).Take (1);
			var task = query.ToListAsync ();
			var items = await task;

			// check...
			Assert.AreEqual (1, items.Count);
			Assert.AreEqual ("0", items[0].FirstName);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableQuerySkip () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();
			await conn.ExecuteAsync("delete from customer");

			// create...
			for (int index = 0; index < 10; index++) {
				var customer = this.CreateCustomer ();
				customer.FirstName = index.ToString ();
				await conn.InsertAsync(customer);
			}

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.FirstName).Skip (5);
			var task = query.ToListAsync ();
			var items = await task;

			// check...
			Assert.AreEqual (5, items.Count);
			Assert.AreEqual ("5", items[0].FirstName);
		});

		[UnityTest]
		public IEnumerator TestAsyncTableElementAtAsync () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();
			await conn.CreateTableAsync<Customer>();
			await conn.ExecuteAsync("delete from customer");

			// create...
			for (int index = 0; index < 10; index++) {
				var customer = this.CreateCustomer ();
				customer.FirstName = index.ToString ();
				await conn.InsertAsync(customer);
			}

			// query...
			var query = conn.Table<Customer> ().OrderBy (v => v.FirstName);
			var task = query.ElementAtAsync (7);
			
			var loaded = await task;

			// check...
			Assert.AreEqual ("7", loaded.FirstName);
		});


		[UnityTest]
		public IEnumerator TestAsyncGetWithExpression() => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection();
			await conn.CreateTableAsync<Customer>();
			await conn.ExecuteAsync("delete from customer");

			// create...
			for (int index = 0; index < 10; index++)
			{
				var customer = this.CreateCustomer();
				customer.FirstName = index.ToString();
				await conn.InsertAsync(customer);
			}

			// get...
			var result = conn.GetAsync<Customer>(x => x.FirstName == "7");
            
			var loaded = await result;
			// check...
			Assert.AreEqual("7", loaded.FirstName);
		});

		[UnityTest]
		public IEnumerator CreateTable () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();

			var trace = new List<string> ();
			conn.Tracer = trace.Add;
			conn.Trace = true;

			var r0 = await conn.CreateTableAsync<Customer>();

			Assert.AreEqual (CreateTableResult.Created, r0);

			var r1 = await conn.CreateTableAsync<Customer> ();

			Assert.AreEqual (CreateTableResult.Migrated, r1);

			var r2 = await conn.CreateTableAsync<Customer> ();

			Assert.AreEqual (CreateTableResult.Migrated, r1);

			Assert.AreEqual (7, trace.Count);
		});

		[UnityTest]
		public IEnumerator CloseAsync () => UniTask.ToCoroutine(async () => 
		{
			var conn = GetConnection ();

			var r0 = await conn.CreateTableAsync<Customer> ();

			Assert.AreEqual (CreateTableResult.Created, r0);

			await conn.CloseAsync();
		});


	}
}