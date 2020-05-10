﻿namespace CinemaWorld.Services.Data.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using CinemaWorld.Data;
    using CinemaWorld.Data.Models;
    using CinemaWorld.Data.Models.Enumerations;
    using CinemaWorld.Data.Repositories;
    using CinemaWorld.Models.InputModels.NewsComments;
    using CinemaWorld.Services.Data.Common;
    using CinemaWorld.Services.Data.Contracts;
    using CinemaWorld.Services.Mapping;

    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class NewsCommentsServiceTests : IDisposable
    {
        private const string TestImageUrl = "https://someurl.com";

        private readonly INewsCommentsService newsCommentsService;
        private EfDeletableEntityRepository<NewsComment> newsCommentsRepository;
        private EfDeletableEntityRepository<News> newsRepository;
        private EfDeletableEntityRepository<CinemaWorldUser> usersRepository;
        private SqliteConnection connection;

        private News firstNews;
        private NewsComment firstNewsComment;
        private CinemaWorldUser user;

        public NewsCommentsServiceTests()
        {
            this.InitializeMapper();
            this.InitializeDatabaseAndRepositories();
            this.InitializeFields();

            this.newsCommentsService = new NewsCommentsService(this.newsCommentsRepository);
        }

        [Fact]
        public async Task TestAddingNewsComment()
        {
            await this.SeedUsers();
            await this.SeedNews();

            var newsComment = new CreateNewsCommentInputModel
            {
                NewsId = this.firstNews.Id,
                Content = "Hello, how are you?",
            };

            await this.newsCommentsService.CreateAsync(newsComment.NewsId, this.user.Id, newsComment.Content);
            var count = this.newsCommentsRepository.All().Count();

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CheckSettingOfNewsCommentProperties()
        {
            await this.SeedUsers();
            await this.SeedNews();

            var model = new CreateNewsCommentInputModel
            {
                NewsId = this.firstNews.Id,
                Content = "Hello, how are you?",
            };

            await this.newsCommentsService.CreateAsync(model.NewsId, this.user.Id, model.Content);

            var newsComment = await this.newsCommentsRepository.All().FirstOrDefaultAsync();

            Assert.Equal(model.NewsId, newsComment.NewsId);
            Assert.Equal("Hello, how are you?", newsComment.Content);
        }

        // TODO
        [Fact]
        public async Task CheckIfAddingNewsCommentThrowsArgumentException()
        {
            this.SeedDatabase();

            var newsComment = new CreateNewsCommentInputModel
            {
                NewsId = this.firstNews.Id,
                Content = "Test comment here",
            };

            var exception = await Assert
                .ThrowsAsync<ArgumentException>(async ()
                    => await this.newsCommentsService
                    .CreateAsync(newsComment.NewsId, this.user.Id, newsComment.Content));

            Assert.Equal(string.Format(ExceptionMessages.NewsCommentAlreadyExists, newsComment.NewsId), exception.Message);
        }

        public void Dispose()
        {
            this.connection.Close();
            this.connection.Dispose();
        }

        private void InitializeDatabaseAndRepositories()
        {
            this.connection = new SqliteConnection("DataSource=:memory:");
            this.connection.Open();
            var options = new DbContextOptionsBuilder<CinemaWorldDbContext>().UseSqlite(this.connection);
            var dbContext = new CinemaWorldDbContext(options.Options);

            dbContext.Database.EnsureCreated();

            this.newsCommentsRepository = new EfDeletableEntityRepository<NewsComment>(dbContext);
            this.usersRepository = new EfDeletableEntityRepository<CinemaWorldUser>(dbContext);
            this.newsRepository = new EfDeletableEntityRepository<News>(dbContext);
        }

        private void InitializeFields()
        {
            this.user = new CinemaWorldUser
            {
                Id = "1",
                Gender = Gender.Male,
                UserName = "pesho123",
                FullName = "Pesho Peshov",
                Email = "test_email@gmail.com",
                PasswordHash = "123456",
            };

            this.firstNews = new News
            {
                Title = "First news title",
                Description = "First news description",
                ShortDescription = "First news short description",
                ImagePath = TestImageUrl,
                UserId = this.user.Id,
                ViewsCounter = 30,
                IsUpdated = false,
            };

            this.firstNewsComment = new NewsComment
            {
                NewsId = this.firstNews.Id,
                Content = "Test comment here",
                UserId = this.user.Id,
            };
        }

        private async void SeedDatabase()
        {
            await this.SeedUsers();
            await this.SeedNews();
        }

        private async Task SeedUsers()
        {
            await this.usersRepository.AddAsync(this.user);

            await this.usersRepository.SaveChangesAsync();
        }

        private async Task SeedNews()
        {
            await this.newsRepository.AddAsync(this.firstNews);

            await this.newsRepository.SaveChangesAsync();
        }

        private void InitializeMapper() => AutoMapperConfig.
            RegisterMappings(Assembly.Load("CinemaWorld.Models.ViewModels"));
    }
}