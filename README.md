# Velox.DB
Portable lightweight .NET ORM for mobile, desktop and servers

Velox.DB is a .NET ORM that can be used on any platform supported by .NET, Mono and Xamarin.

Features:
- Works on any .NET 4.5+ platform, including Xamarin (iOS and Android), Windows Phone 8 and Windows 8 (Store Apps)
- Lightweight (assembly is < 140k), very fast and without any dependencies
- Full LINQ expression support, including complex expressions involving relations
- Seamless support for relations (many-to-one and one-to-many)
- Uses POCO classes (no base class or interface required)
- Works with any storage backend, including SQL databases and non-relational databases, in-memory storage and flat files (Json/Xml)
- Built-in support for many databases. Adding other database providers is extremely easy.
  - Sqlite (Windows, iOS, Android, Windows Phone, Windows 8)
  - SQL Server (Windows, iOS and Android)
  - MySql (Windows)
  - More to come...

### Velox.DB in action

Defining your object classes:

```csharp
public class Customer
{
   [Column.PrimaryKey(AutoIncrement = true)]
   public int CustomerID;
   [Column.NotNull]
   public string Name;
   
   public IDataSet<Order> Orders;
}

public class Order
{
   [Column.PrimaryKey(AutoIncrement = true)]
   public int OrderID;
   public int CustomerID;
   public DateTime Date;
   
   public Customer Customer;
}
```

- *Note 1: You can use public fields or properties*
- *Note 2: There's actually no need to use the PrimaryKey attribute because you can define naming conventions for your classes. The default naming convention handles the case above.*

Connect to a storage backend:

```csharp
var dbContext = new Vx.Context(new SqliteDataProvider("mydb.sqlite"));

// Create tables (if they don't exist) yet

dbContext.CreateTable<Customer>();
dbContext.CreateTable<Order>();
```

Create a record in the database:

```csharp
Customer customer = new Customer { Name = "John Doe" };

dbContext.Save(customer);

Console.WriteLine("Created customer ID {0}", customer.CustomerID");
```

Add a related record:

```csharp
Order order = new Order { Date = DateTime.Today, Customer = customer };

dbContext.Save(order);
```

Read relations:

```csharp
Customer customer = DB.Customers.Read(1);

foreach (Order order in customer.Orders)
   Console.WriteLine("Order ID = {0}" , order.OrderID);

// The one-to-many relation was automatically (lazy) populated because
// it was declared as DataSet<T>. You can declare relations using any 
// collection type, like IEnumerable<Order> or Order[] but you
// would need to tell Velox.DB explicitly to read the relation 
// by calling Vx.LoadRelations(...) - the same is true for
// many-to-one relations
```

Reading data from the database requires a DataSet, which can be retrieved by calling DataSet<T> from the context:

```csharp
var customers = from customer in dbContext.DataSet<Customer>() 
                   order by customer.Name 
                   select customer;
```

Ok, so having to use dbContext.DataSet<Customer>() may be a little too much typing...

```csharp
var DB = new {
                Customers = dbContext.DataSet<Customer>(),
                Orders = dbContext.DataSet<Order>()
             };
// This works because DataSets are immutable and lightweight objects that are 
// bound to the data store

// Now it's a little easier (but there are other ways too)

var customers = from customer in DB.Customers
                   order by customer.Name 
                   select customer;
```


LINQ queries:

```csharp

var customers = from customer in DB.Customers 
                   where customer.Orders.Any() 
                   order by customer.Name 
                   select customer;
                   
// This query is automatically translated to correct SQL 
// if the data provider supports it (as all built-in
// SQL providers do):
//
// select * from Customer c 
//     where exists (select * from Order o where o.CustomerID=c.CustomerID) 
//     order by c.Name
```

Complex cross-relation LINQ queries:

```csharp

// Get all orders belonging to customers whos name start with "A"
var orders = from order in DB.Orders 
                   where order.Customer.Name.StartsWith("A")
                   select order;
                   
// Translates to:
//
// select * from Order o 
//          inner join Customer c on c.CustomerID=o.CustomerID 
//          where c.Name like 'A%'
```

So what if a query can't be translated to SQL? In that case, any part of the query that can't be translated will be evaluated in code. This will hurt performance but it will still give you the results you need.

For example, say you have a custom function to determine if a customer object should be included in a query, but you only want to select customers with a name starting with "A":

```csharp
var customers = from customer in DB.Customers 
                where customer.Name.StartsWith("A") && CustomMethod(customer) 
                select customer;

// It's obvious that the call to CustomMethod(customer) can't be 
// translated to SQL so Velox.DB will do the following:
//
//      select * from Customer where Name like 'A%'
// Then the results will filtered in code:
//      customers = customers.Where(c => CustomMethod(c))
```
Velox.DB will split the predicate and feed part of if to the database and part of it will be evaluated in code.

##### Ad-Hoc SQL queries

If your data provider supports it, you can send queries directly to the database and store the results in an object:

```csharp
public class CustomerInfo
{
    public int CustomerID;
    public string CustomerName;
    public int NumOrders;
}

var records = dbContext.Query<CustomerInfo>(
                  "select c.CustomerID,c.Name as CustomerName, count(*) from Customer c " +
                  "inner join Order o on o.CustomerID=c.CustomerID " +
                  "group by c.CustomerID,c.Name");
```
##### Async

All methods can be used asynchronously, for example:

```csharp

// Create a table asynchronously
await dbContext.CreateTableAsync<Customer>();

// Asynchronously fetch a list of customers. Note that we have to use ToArray() or
// ToList() because it's not possible to use IEnumerable<Customer> for asynchronous
// operations
var customers1 = await DB.Customers.Where(c => c.Name.StartsWith("A")).Async().ToArray();
var customers2 = await DB.Customers.Async().Where(c => c.Name.StartsWith("A")).ToArray();

var customers3 = await DB.Customers.Async().Count();

var customer = await DB.Customers.Async().Read(1);
```

When you call Async() on a DataSet, all operations on the dataset that actually fetch data will be executed asynchronously. You can go back to a synchronous DataSet by calling Sync():

```csharp

var asyncCustomers = DB.Customers.Async();

var numCustomers = await asyncCustomers.Count();
var aCustomers = asyncCustomers.Where(c => c.Name.StartsWith("A"));

var customerList = await aCustomers.ToArray(); // aCustomers is an async DataSet

// go back to a synchronous DataSet
foreach (Customer c in aCustomers.Sync())
{
   // We can synchronously enumerate over the DataSet again
}
```

#### Documentation

Full documention is in the works. A good part is already done and can be found here:

https://github.com/velox/DB/wiki
