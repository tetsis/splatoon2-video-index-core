﻿using ApplicationService.Test.Mocks;
using ApplicationService.Users.Exceptions;
using ApplicationService.Videos;
using ApplicationService.Videos.Add;
using ApplicationService.Videos.Commons;
using ApplicationService.Videos.Export;
using ApplicationService.Videos.Get;
using ApplicationService.Videos.GetInfo;
using ApplicationService.Videos.Remove;
using DomainModel.Channels;
using DomainModel.EditingHistories;
using DomainModel.Users;
using DomainModel.Videos;
using DomainModel.Videos.Rules;
using EFInfrastructure.Persistence.Channels;
using EFInfrastructure.Persistence.EditingHistories;
using EFInfrastructure.Persistence.Users;
using EFInfrastructure.Persistence.Videos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationService.Test.Videos
{
    [TestClass]
    public class VideoApplicationServiceTest : ApplicationServiceTestBase
    {
        private IVideoFactory _videoFactory;
        private IVideoRepository _videoRepository;
        private IChannelFactory _channelFactory;
        private IChannelRepository _channelRepository;
        private IUserRepository _userRepository;
        private IEditingHistoryRepository _editingHistoryRepository;
        private IVideoApplicationService _videoApplicationService;
        private User editor, maintainer, admin;

        [TestInitialize]
        public void TestInitialize()
        {
            _videoFactory = new MockVideoFactory();
            _videoRepository = new EFVideoRepository(DbContext);
            _channelFactory = new MockChannelFactory();
            _channelRepository = new EFChannelRepository(DbContext);
            _userRepository = new EFUserRepository(DbContext);
            _editingHistoryRepository = new EFEditingHistoryRepository(DbContext);
            _videoApplicationService = new VideoApplicationService(_videoFactory, _videoRepository, _channelFactory, _channelRepository, _userRepository, _editingHistoryRepository);

            editor = new User("editor", "editor", "image", "oauthToken", "oauthTokenSecret", Role.Editor);
            _userRepository.Create(editor);

            maintainer = new User("maintainer", "maintainer", "image", "oauthToken", "oauthTokenSecret", Role.Maintainer);
            _userRepository.Create(maintainer);

            admin = new User("admin", "admin", "image", "oauthToken", "oauthTokenSecret", Role.Administrator);
            _userRepository.Create(admin);
        }

        [TestMethod]
        public void 動画を追加する()
        {
            var command = new VideoAddCommand
            {
                UserId = admin.Id,
                SessionId = admin.SessionId,
                Video = new VideoAddData
                {
                    Id = "videoId",
                    Battles = new List<BattleAddData>
                    {
                        new BattleAddData
                        {
                            Seconds = 10,
                            Rule = "SplatZones",
                            Stage = "TheReef",
                            Weapon = "Sploosh_o_matic"
                        },
                        new BattleAddData
                        {
                            Seconds = 20,
                            Rule = "SplatZones",
                            Stage = "TheReef",
                            Weapon = "Sploosh_o_matic"
                        }
                    }
                }
            };
            _videoApplicationService.Add(command);

            var video = _videoRepository.Find("videoId");

            Assert.IsNotNull(video);
            Assert.AreEqual("title", video.VideoInfo.Title);
            Assert.AreEqual(2, video.Battles.Count);
            Assert.AreEqual(RuleId.SplatZones, video.Battles[0].Rule.Id);

            var channel = _channelRepository.Find(video.VideoInfo.ChannelId);
            Assert.IsNotNull(channel);

            var editingHistory = _editingHistoryRepository.FindRange(0, 10);
            Assert.AreEqual(1, editingHistory.Count);
        }

        [TestMethod]
        public void 動画を削除する()
        {
            var videoInfo = new VideoInfo("channelId", "title", "thumbnail", DateTime.Now, 1);
            var video = new Video("id", videoInfo, null);
            _videoRepository.Create(video);

            var command = new VideoRemoveCommand
            {
                UserId = admin.Id,
                SessionId = admin.SessionId,
                VideoId = "id"
            };
            _videoApplicationService.Remove(command);

            var removedVideo = _videoRepository.Find("id");

            Assert.IsNull(removedVideo);

            var channel = _channelRepository.Find(video.VideoInfo.ChannelId);
            Assert.IsNull(channel);
        }

        [TestMethod]
        public void 認証に失敗すると動画削除できない()
        {
            bool exceptionOccured = false;
            try
            {
                var videoInfo = new VideoInfo("channelId", "title", "thumbnail", DateTime.Now, 1);
                var video = new Video("id", videoInfo, null);
                _videoRepository.Create(video);

                var command = new VideoRemoveCommand
                {
                    UserId = editor.Id,
                    SessionId = "aaa",
                    VideoId = "id"
                };
                _videoApplicationService.Remove(command);
            }
            catch (UserIsNotAuthenticatedException)
            {
                exceptionOccured = true;
            }

            Assert.IsTrue(exceptionOccured);
        }

        [TestMethod]
        public void エディターは動画削除できない()
        {
            bool exceptionOccured = false;
            try
            {
                var videoInfo = new VideoInfo("channelId", "title", "thumbnail", DateTime.Now, 1);
                var video = new Video("id", videoInfo, null);
                _videoRepository.Create(video);

                var command = new VideoRemoveCommand
                {
                    UserId = editor.Id,
                    SessionId = editor.SessionId,
                    VideoId = "id"
                };
                _videoApplicationService.Remove(command);
            }
            catch (UserIsNotAuthorizedException)
            {
                exceptionOccured = true;
            }

            Assert.IsTrue(exceptionOccured);
        }

        [TestMethod]
        public void 動画データを取得する()
        {
            var addCommand = new VideoAddCommand
            {
                UserId = admin.Id,
                SessionId = admin.SessionId,
                Video = new VideoAddData
                {
                    Id = "videoId",
                    Battles = new List<BattleAddData>
                        {
                            new BattleAddData
                            {
                                Seconds = 10,
                                Rule = "SplatZones",
                                Stage = "TheReef",
                                Weapon = "Sploosh_o_matic"
                            }
                        }
                }
            };
            _videoApplicationService.Add(addCommand);

            var command = new VideoGetCommand
            {
                VideoId = "videoId"
            };
            var video = _videoApplicationService.Get(command).Video;

            Assert.AreEqual("ガチエリア", video.Battles[0].Rule.Name);
            Assert.AreEqual("バッテラストリート", video.Battles[0].Stage.Name);
            Assert.AreEqual("ボールドマーカー", video.Battles[0].Weapon.Name);
        }

        [TestMethod]
        public void 動画情報のみ取得する()
        {
            var command = new VideoGetInfoCommand
            {
                VideoId = "id"
            };
            var result = _videoApplicationService.GetInfo(command);

            Assert.AreEqual(1, result.VideoInfo.ViewCount);
        }

        [TestMethod]
        public void エクスポートする()
        {
            // たくさん登録する
            for (int i = 0; i < 100; i++)
            {
                var addCommand = new VideoAddCommand
                {
                    UserId = admin.Id,
                    SessionId = admin.SessionId,
                    Video = new VideoAddData
                    {
                        Id = $"videoId{i}",
                        Battles = new List<BattleAddData>
                        {
                            new BattleAddData
                            {
                                Seconds = 10,
                                Rule = "SplatZones",
                                Stage = "TheReef",
                                Weapon = "Sploosh_o_matic"
                            },
                            new BattleAddData
                            {
                                Seconds = 20,
                                Rule = "SplatZones",
                                Stage = "MusselforgeFitness",
                                Weapon = "Sploosh_o_matic"
                            }
                        }
                    }
                };
                _videoApplicationService.Add(addCommand);
            }

            // エクスポートする
            var command = new VideoExportCommand
            {
                UserId = admin.Id,
                SessionId = admin.SessionId,
            };
            var result = _videoApplicationService.Export(command);

            Assert.AreEqual(100, result.Videos.Count);
            Assert.AreEqual("videoId0", result.Videos[0].Id);
            Assert.AreEqual(10, result.Videos[0].Battles[0].Seconds);
            Assert.AreEqual("MusselforgeFitness", result.Videos[0].Battles[1].Stage);
        }
    }
}
