# DapperExtensions
Dapper拓展，需要Dapper版本≥1.50,本类库基于Dapper拓展了5种数据库,分别为
SqlServer、MySQL、SQLite、PostgreSQL、Oracle(由于Oracle拓展后没有经过测试，有BUG还望指正)

使用方法
1、首先是实体层
using DapperExtensions;

Table("People")]  //表示表名为People，默认不填的话用类名作为表名
public class People
{
    [Key(true)]  //[Key(true)] 表示主键，并且是自增。 [Key(false)] //表示主键，非自增。
    public int id { get; set; } //这些不标注的属性，表示数据库字段
    public string name { get; set; }
    public int sex { get; set; }
    public string age { get; set; }

    [Computed] //[Computed] 表示这个属性为非数据库字段，在拓展方法Insert UpdateById等中，就不会把忽略列加进去
    public string ok { get; set; }
    [Computed]
    public string address { get; set; }
    [Computed]
    public string xxx { get; set; }
}

2、开始使用，5种数据库分别引用为
using DapperExtensions.SqlServerExt;
using DapperExtensions.MySQLExt;
using DapperExtensions.SqLiteExt;
using DapperExtensions.PostgreSQLExt;
using DapperExtensions.OracleExt;
选择你要访问的数据库类型using，这边我Sqlserver为例子
using DapperExtensions.SqlServerExt;

public static IDbConnection GetConn() //获取sql数据库连接，这边你可以用MySql、SQLlite等五种数据库Connection
{
    string str = "server=.;database=test;uid=sa;pwd=123";
    SqlConnection conn = new SqlConnection(str);
    conn.Open();
    return conn;
}

using (var conn = GetConn()) 
{
    People p = new People();
    p.name = "张三";
    p.sex = 100;
    p.age = "我100岁了";
    dynamic result = conn.Insert(p); //拓展了Insert方法
    p = conn.GetById<People>(1);  //GetById方法
}

对于conn还有很多方法进行了拓展，修改，删除，查询，分页等等。就不一一演示了。
