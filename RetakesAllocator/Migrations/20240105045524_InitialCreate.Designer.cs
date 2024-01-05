﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RetakesAllocator.db;

#nullable disable

namespace RetakesAllocator.Migrations
{
    [DbContext(typeof(Db))]
    [Migration("20240105045524_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.14");

            modelBuilder.Entity("RetakesAllocator.db.UserSetting", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("WeaponPreferences")
                        .HasMaxLength(10000)
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.ToTable("UserSettings");
                });
#pragma warning restore 612, 618
        }
    }
}
