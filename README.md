# DapperExtensions
Dapper拓展方法，需要Dapper版本≥1.50,本类库基于Dapper对五种数据库进行了拓展,分别为
SqlServer、MySQL、SQLite、PostgreSQL、Oracle(由于Oracle拓展后没有经过测试，有BUG还望指正)，对于含有组合主键的表，拓展方法有些不能用，
暂且支持单主键的表。对于组合主键表就直接利用Dapper原生Query、Execute、ExecuteScalar等方法进行操作。

<pre>
<code>
Dapper下载地址：
https://www.nuget.org/packages/Dapper/
https://github.com/StackExchange/dapper-dot-net
</code>
</pre>
使用方法
1、首先建立实体层
<pre>
<code>
using DapperExtensions;

Table("People")]  //表示表名为People，默认不填的话用类名作为表名
public class People
{
    [Key(true)]  //[Key(true)] 表示主键，并且是自增identity。 [Key(false)] //表示主键，非自增no identity。
    public int id { get; set; } 
    public string name { get; set; } //这些不标注的属性，表示数据库字段
    public int sex { get; set; }
    public string age { get; set; }
    
    [Computed] //[Computed] 表示这个属性为非数据库字段，在拓展方法Insert Update等中，就不会把该属性传递进去
    public string ok { get; set; }
    [Computed]
    public string address { get; set; }
    [Computed]
    public string xxx { get; set; }
}
</code>
</pre>
2、开始使用，五种数据库拓展的命名空间分别为
<pre>
<code>
using DapperExtensions.SqlServerExt;
using DapperExtensions.MySQLExt;
using DapperExtensions.SqLiteExt;
using DapperExtensions.PostgreSQLExt;
using DapperExtensions.OracleExt;

选择你要访问的数据库进行命名空间的引用，这边我以Sqlserver为例子

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
    
    int result = conn.InsertIdentity(p); //插入自增键方法
    
    People pp = conn.GetById<People>(1);  //GetById方法
    
    var list = conn.GetByIds<People>(new int[] { 1, 2, 3} );//根据id串获取实体列表
    
    var peopleList = conn.GetAll<People>(); //获取整张表数据
    
    int result = conn.DeleteById<People>(1); //根据主键id删除数据
    
    int result = conn.DeleteByIds<People>(new int[] { 1, 3 }); //根据id串删除数据
    
    int result = conn.DeleteAll<People>(); //删除整张表数据
    
    People p = new People();
    p.id = 1;
    p.name = "张三";
    p.sex = 27;
    p.age = "我27岁";
    int result = conn.UpdateById(p); //根据主键修改数据，可以指定要修改的字段
    
    long total = conn.GetTotal<People>(); //获取记录总数。可以指定查询条件
    
    var data = conn.GetBySkip<People>(1, 3);//分页，可以指定查询条件
    
    var data = conn.GetByPageIndex<People>(1,3); //分页。可以指定查询条件
    
    People p = new People();
    p.id = -1;
    p.name = "蝴蝶";
    p.sex = 11;
    p.age = "我11岁了";
    int result = conn.InsertOrUpdate(sc); //根据主键存在则更新，不存在则插入
    
    int result = conn.DeleteByWhere<People>(); //根据查询条件删除
    
    int result = conn.UpdateByWhere<People>(); //根据查询条件修改数据
    
    var result = conn.GetByWhere<People>(); //根据查询条件返回People表数据
    
    var tran = conn.BeginTransaction();
    try
    {
        conn.BulkCopy<People>(dt, tran);  //大批量数据插入
        
        conn.BulkUpdate<People>(dt,tran); //根据主键大批量数据更新
        
        tran.Commit();
    }
    catch
    {
        tran.Rollback();
    }
    tran.Dispose();
    .......更多方法就不一一列举了
}
</code>
</pre>
对于conn总共进行了30几个拓展方法，修改，删除，查询，分页等等。
在V2.1版本中提供了CodeSmith模板，更快速的生成Model和DAL层，更快速度的搭建项目，下载地址：
<br/>最新版：https://github.com/znyet/DapperExtensions/releases
<br/>CodeSmith模板：https://github.com/znyet/DapperExtensions/releases/tag/V2.1
