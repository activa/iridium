# Velox.DB
Portable lightweight .NET ORM for mobile, desktop and servers

Velox.DB is a .NET ORM that can be used on any platform supported by .NET, Mono and Xamarin.

Features:
- Works on any .NET 4.5+ platform, including Xamarin and Mono
- Lightweight and very fast
- Full LINQ expression support, including complex expressions involving relations
- Seamless support for relations (many-to-one and one-to-many)
- Uses POCO classes (no base class required)
- Works with any storage backend, including non-relational databases and flat files (Json/Xml)

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

(note that you can use public fields or properties)

Connect to a storage backend:

```csharp
var dbContext = new Vx.Context(new SqliteDataProvider("mydb.sqlite"));

// Create tables

dbContext.CreateTable<Customer>();
dbContext.CreateTable<Order>();
```

Create some records in the database:

```csharp

Customer customer1 = new Customer { Name = "John Doe" };

dbContext.DataSet<Customer>.Save(customer1);

Console.WriteLine("Created customer ID {0}", customer1.CustomerID");
```

Add a related record:

```csharp
Order order = new Order { Date = DateTime.Today, Customer = customer1 };

dbContext.DataSet<Orders>.Save(order);
```

Read relations:

```csharp
Customer customer = dbContext.DataSet<Customer>().Read(1);

foreach (Order order in customer.Orders)
   Console.WriteLine("Order ID = {0}" , order.OrderID);
```

LINQ queries:

```csharp

var customers = from customer in dbContext.DataSet<Customer> 
                   where customer.Orders.Any() 
                   order by customer.Name 
                   select customer;
                   
// This query is automatically translated to correct SQL if the data provider supports it:
//
// select * from Customer c where exists (select * from Order o where o.CustomerID=c.CustomerID) order by c.Name
```

Complex cross-relation LINQ queries:

LINQ queries:

```csharp

// Get all orders belonging to customers whos name start with "A"
var orders = from order in dbContext.DataSet<Order> 
                   where order.Customer.Name.StartsWith("A")
                   select order;
                   
// Translates to:
//
// select * from Order o inner join Customer c on c.CustomerID=o.CustomerID where c.Name like 'A%'
```
