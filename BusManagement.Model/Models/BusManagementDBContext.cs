using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BusManagement.Model.Entity;

namespace BusManagement.Model.Models;

public partial class BusManagementDBContext : DbContext
{
    public BusManagementDBContext()
    {
    }

    public BusManagementDBContext(DbContextOptions<BusManagementDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TM_USER> TM_USERs { get; set; }
    public virtual DbSet<TM_MENU> TM_MENUs { get; set; }
    public virtual DbSet<TM_MENU_ROLE> TM_MENU_ROLEs { get; set; }
    public virtual DbSet<TM_ROLE> TM_ROLEs { get; set; }
    public virtual DbSet<Model.Entity.TM_EMPLOYEE> TM_EMPLOYEEs { get; set; }
    public virtual DbSet<TM_EMPLOYEE_ROUTE> TM_EMPLOYEE_ROUTEs { get; set; }
    public virtual DbSet<TM_EMPLOYEE_ROUTE_OT> TM_EMPLOYEE_ROUTE_OTs { get; set; }
    public virtual DbSet<TM_EMPLOYEE_ROUTE_HISTORY> TM_EMPLOYEE_ROUTE_HISTORYs { get; set; }
    public virtual DbSet<TM_BUS_STOP> TM_BUS_STOPs { get; set; }
    public virtual DbSet<TM_BUS_ROUTE> TM_BUS_ROUTEs { get; set; }
    public virtual DbSet<TM_BUS_ROUTE_ASSIGN> TM_BUS_ROUTE_ASSIGNs { get; set; }
    public virtual DbSet<TM_BUS_EVALUATION> TM_BUS_EVALUATIONs { get; set; }
    public virtual DbSet<TM_BUS_ROUTE_STOP> TM_BUS_ROUTE_STOPs { get; set; }
    public virtual DbSet<TM_BUS> TM_BUSs { get; set; }
    public virtual DbSet<TM_TAXI> TM_TAXIs { get; set; }
    public virtual DbSet<TM_DRIVER> TM_DRIVERs { get; set; }
    public virtual DbSet<TM_DRIVER_VIOLATION> TM_DRIVER_VIOLATIONs { get; set; }
    public virtual DbSet<TM_VENDOR> TM_VENDORs { get; set; }
    public virtual DbSet<TM_REQUEST_TYPE> TM_REQUEST_TYPEs { get; set; }
    public virtual DbSet<TM_BUS_REQUEST> TM_BUS_REQUESTs { get; set; }
    public virtual DbSet<TM_BUS_REQUEST_FAILED> TM_BUS_REQUEST_FAILEDs { get; set; }
    public virtual DbSet<TM_APPROVAL_HISTORY> TM_APPROVAL_HISTORYs { get; set; }
    public virtual DbSet<TM_TRIP_REQUEST> TM_TRIP_REQUESTs { get; set; }
    public virtual DbSet<TM_BUS_OT> TM_BUS_OTs { get; set; }
    public virtual DbSet<TM_BUS_OT_ASSIGN> TM_BUS_OT_ASSIGNs { get; set; }
    public virtual DbSet<TM_CARD> TM_CARDs { get; set; }
    public virtual DbSet<TM_CARD_USAGE> TM_CARD_USAGEs { get; set; }
    public virtual DbSet<TM_CARD_BORROW> TM_CARD_BORROWs { get; set; }
    public virtual DbSet<TM_CARD_PRINT_QUEUE> TM_CARD_PRINT_QUEUEs { get; set; }
    public virtual DbSet<TM_MASTER_MAIL> TM_MASTER_MAILs { get; set; }
    public virtual DbSet<TM_DIAGRAM> TM_DIAGRAMs { get; set; }
    public virtual DbSet<TM_CITY> TM_CITYs { get; set; }
    public virtual DbSet<TM_WARD> TM_WARDs { get; set; }
    public virtual DbSet<TM_SHUTTLE_LOCATION> TM_SHUTTLE_LOCATIONs { get; set; }
    public virtual DbSet<TM_SHUTTLE_SCHEDULE> TM_SHUTTLE_SCHEDULEs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        var appSettingsBasePath = File.Exists(Path.Combine(currentDirectory, "appsettings.json"))
            ? currentDirectory
            : Path.Combine(currentDirectory, "BusManagement");

        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(appSettingsBasePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("BusManagementConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'BusManagementConnection' was not found in appsettings.");
        }

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
