﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TelegramBot.Domain;

#nullable disable

namespace TelegramBot.Migrations
{
    [DbContext(typeof(ApplicationContext))]
    partial class ApplicationContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.1");

            modelBuilder.Entity("AdminProfileEvent", b =>
                {
                    b.Property<long>("AdminProfileId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("EventId")
                        .HasColumnType("TEXT");

                    b.HasKey("AdminProfileId", "EventId");

                    b.HasIndex("EventId");

                    b.ToTable("AdminProfileEvent");
                });

            modelBuilder.Entity("TelegramBot.Domain.Entities.Event", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("TelegramBot.Domain.Entities.Person", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LastProfileMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("role")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Persons");

                    b.UseTpcMappingStrategy();
                });

            modelBuilder.Entity("UserProfileEvent", b =>
                {
                    b.Property<Guid>("EventId")
                        .HasColumnType("TEXT");

                    b.Property<long>("UserProfileId")
                        .HasColumnType("INTEGER");

                    b.HasKey("EventId", "UserProfileId");

                    b.HasIndex("UserProfileId");

                    b.ToTable("UserProfileEvent");
                });

            modelBuilder.Entity("TelegramBot.Domain.Entities.AdminProfile", b =>
                {
                    b.HasBaseType("TelegramBot.Domain.Entities.Person");

                    b.Property<int>("AdminState")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("CurrentEvent")
                        .HasColumnType("TEXT");

                    b.ToTable("AdminProfiles");
                });

            modelBuilder.Entity("TelegramBot.Domain.Entities.UserProfile", b =>
                {
                    b.HasBaseType("TelegramBot.Domain.Entities.Person");

                    b.Property<bool>("HeIsEighteen")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsRegistered")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT");

                    b.Property<int>("UserState")
                        .HasColumnType("INTEGER");

                    b.ToTable("UserProfiles");
                });

            modelBuilder.Entity("AdminProfileEvent", b =>
                {
                    b.HasOne("TelegramBot.Domain.Entities.AdminProfile", null)
                        .WithMany()
                        .HasForeignKey("AdminProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TelegramBot.Domain.Entities.Event", null)
                        .WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("UserProfileEvent", b =>
                {
                    b.HasOne("TelegramBot.Domain.Entities.Event", null)
                        .WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TelegramBot.Domain.Entities.UserProfile", null)
                        .WithMany()
                        .HasForeignKey("UserProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
