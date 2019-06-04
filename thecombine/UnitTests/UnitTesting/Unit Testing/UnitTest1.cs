using Microsoft.VisualStudio.TestTools.UnitTesting;
using BackendFramework.ValueModels;
using BackendFramework.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using BackendFramework.Context;
using static BackendFramework.Startup;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using Moq;
using System.Threading;
using MongoDB.Driver.Core.Operations;
using System;
using BackendFramework.Interfaces;

namespace Unit_Testing
{
    
    [TestClass]
    public class UnitTests
    {

        public WordService ServiceBuilder
        {
            get {
                Mock<IMongoCollection<Word>> mockIMongoCollection = new Mock<IMongoCollection<Word>>(MockBehavior.Strict);

                //copied from https://stackoverflow.com/questions/48176063/mongodb-c-sharp-driver-mock-method-that-returns-iasynccursor
                Mock<IAsyncCursor<Word>> mockCursor = new Mock<IAsyncCursor<Word>>();
                mockCursor.Setup(_ => _.Current).Returns(new List<Word>());
                mockCursor
                    .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                    .Returns(true)
                    .Returns(false);
                mockCursor
                    .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(true))
                    .Returns(Task.FromResult(false));
                
                mockIMongoCollection.Setup(p => p.InsertOneAsync(It.IsAny<Word>(), null, default))
                    .Returns((Word x, InsertOneOptions y, CancellationToken z) => Task<Word>.Factory.StartNew(() => x));
                //mockIMongoCollection.Setup(p => p.Find(It.IsAny<string>(), It.IsAny<FindOptions>()))
                //    .Returns(/*needs an IFindFluent<Word,Word>*/);
                //FindAll expecing ExpressionFilterDefinition<Word> instead of string

                //mockIMongoCollection.Setup(p => p.FindAsync(It.IsAny<string>(), null, default))
                //    .Returns(Task<IAsyncCursor<Word>>.Factory.StartNew(() => mockCursor.Object));
                //mockIMongoCollection.Setup(p => p.FindAsync(It.IsAny<FilterDefinition<Word>>(), null, default))
                //    .Returns(Task<IAsyncCursor<Word>>.Factory.StartNew(() => mockCursor.Object));

                mockIMongoCollection.Setup(p => p.DeleteOneAsync(It.IsAny<string>(), default))
                    .Returns(Task<DeleteResult>.Factory.StartNew(() => new DeleteResult.Acknowledged(1)));
                mockIMongoCollection.Setup(p => p.DeleteOneAsync(It.IsAny<JsonFilterDefinition<Word>>(), default))
                    .Returns(Task<DeleteResult>.Factory.StartNew(() => new DeleteResult.Acknowledged(1)));
                //mockIMongoCollection.Setup(p => p.DeleteOneAsync(It.IsAny<SimpleFilterDefinition<Word, string>>(), It.IsAny<CancellationToken>()))
                //    .Returns(Task<DeleteResult>.Factory.StartNew(() => new DeleteResult.Acknowledged(1)));

                Mock<IWordContext> mockWordContext = new Mock<IWordContext>(MockBehavior.Strict);
                mockWordContext.Setup(p => p.Words).Returns(mockIMongoCollection.Object);
                WordService service = new WordService(mockWordContext.Object);
                return service;

                //TODO: VerifyAll for mock
            }
        }

        public async Task<string[]> SetUpDatabase(WordService service)
        {
            //if there are any words in the database, we want to delete them
            Task<List<Word>> getTask = service.GetAllWords();
            List<Word> getList = await getTask;
            foreach (Word word in getList)
            {
                bool deleted = await service.Delete(word.Id);
                if (!deleted) throw new System.Exception("Item not deleted!");
            }

            //let's always have these two words in the database
            Word word1 = new Word();
            word1.Vernacular = "One";
            word1.Gloss = 1;
            word1.Audio = "audio1.mp4";
            word1.Timestamp = "1:00";

            Word word2 = new Word();
            word2.Vernacular = "Two";
            word2.Gloss = 2;
            word2.Audio = "audio2.mp4";
            word2.Timestamp = "2:00";

            //since the ids will change every time, I'm going to return them for easy reference
            string[] idList = new string[2];
            word1 = await service.Create(word1);
            word2 = await service.Create(word2);
            idList[0] = word1.Id;
            idList[1] = word2.Id;

            return idList;
        }

        [TestMethod]
        public async Task TestGetAllWords()
        {
            //Test with empty database
            WordService service = ServiceBuilder; //build an empty database

            Task<List<Word>> getTask = service.GetAllWords(); //This is probably how to do async tasks...?
            List<Word> getList = await getTask; //get the actual list of items returned by the action
            Assert.AreEqual(getList.Count, 0); //empty database should have no entries
            Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => getList[0]); 
            //indexing into an empty list should throw an exception here

            //Test with populated database
            string[] idList = SetUpDatabase(service).Result; 
            //populates the database and gives us the ids of the members

            getTask = service.GetAllWords(); 
            getList = await getTask;

            Assert.AreEqual(getList.Count, 2); //this time, there should be two entries

            Word wordInDb1 = getList[0];
            Word wordInDb2 = getList[1];

            //let's check that everything is right about them
            Assert.AreEqual(wordInDb1.Id, idList[0]);
            Assert.AreEqual(wordInDb1.Vernacular, "One");
            Assert.AreEqual(wordInDb1.Gloss, 1);
            Assert.AreEqual(wordInDb1.Audio, "audio1.mp4");
            Assert.AreEqual(wordInDb1.Timestamp, "1:00");

            Assert.AreEqual(wordInDb2.Id, idList[1]);
            Assert.AreEqual(wordInDb2.Vernacular, "Two");
            Assert.AreEqual(wordInDb2.Gloss, 2);
            Assert.AreEqual(wordInDb2.Audio, "audio2.mp4");
            Assert.AreEqual(wordInDb2.Timestamp, "2:00");

            //indexing to the third element should throw an exception
            Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => getList[2]);

        }


        //the following tests have not yet been configured to be async, 
        //I'm not sure when to await a task

        [TestMethod]
        public async Task TestGetWord()
        {
            //Test with empty database
            WordService service = ServiceBuilder; //build an empty database

            Task<List<Word>> getTask = service.GetWord("111111111111111111111111"); //this is not an id
            List<Word> getList = await getTask;
            Assert.IsNull(getList); //we shouldn't have found anything

            Assert.ThrowsException<System.FormatException>(() => service.GetWord("1"));
            //this is not a valid id, which gives us an exception

            //Test with populated database
            string[] idList = SetUpDatabase(service).Result;
            //populates the database and gives us the ids of the members

            string wordId = idList[0];

            getTask = service.GetWord(wordId); //fetch out the first word in the database
            getList = await getTask;
            Word result = getList[0];

            //make sure we got everything correctly
            Assert.AreEqual(result.Vernacular, "One");
            Assert.AreEqual(result.Gloss, 1);
            Assert.AreEqual(result.Audio, "audio1.mp4");
            Assert.AreEqual(result.Timestamp, "1:00");

            getTask = service.GetWord("111111111111111111111111");
            getList = await getTask;
            Assert.IsNull(getList);

            Assert.ThrowsException<System.FormatException>(() => service.GetWord("1"));

            
        }

        [TestMethod]
        public async Task TestCreate()
        {
            //Test with empty database
            WordService service = ServiceBuilder; //build an empty database

            //TODO: Is there something to test here...?

            //Test with populated database
            string[] idList = SetUpDatabase(service).Result;
            //populates the database

            //make a couple of new words, one filled and one empty
            Word newWord1 = new Word();
            newWord1.Vernacular = "Hello";
            newWord1.Gloss = 5;
            newWord1.Audio = "N/A";
            newWord1.Timestamp = "4:30";

            Word emptyWord = new Word();

            //add them
            newWord1 = await service.Create(newWord1);
            emptyWord = await service.Create(emptyWord);

            //let's see if everything got in there correctly
            Task<List<Word>> getTask = service.GetAllWords();
            List<Word> getList = await getTask;

            Assert.AreEqual(getList.Count, 4);
            Word wordInDb1 = getList[2];
            Word wordInDb2 = getList[3];

            Assert.AreEqual(wordInDb1.Vernacular, "Hello");
            Assert.AreEqual(wordInDb1.Gloss, 5);
            Assert.AreEqual(wordInDb1.Audio, "N/A");
            Assert.AreEqual(wordInDb1.Timestamp, "4:30");

            Assert.IsNull(wordInDb2.Vernacular); //there should be nothing there for the empty word
            Assert.IsNull(wordInDb2.Gloss);
            Assert.IsNull(wordInDb2.Audio);
            Assert.IsNull(wordInDb2.Timestamp);

            //TODO: Perhaps a phoney attribute?
        }

        [TestMethod]
        public async Task TestUpdate()
        {
            //Test with empty database
            WordService service = ServiceBuilder; //build an empty database

            await service.Update("111111111111111111111111");
            //TODO: Assert result WriteResult has error code

            Assert.ThrowsException<System.FormatException>(() => service.Update("1"));

            //Test with populated database
            string[] idList = SetUpDatabase(service).Result;
            //populates the database and gives us the ids of the members

            Task<List<Word>> getTask = service.GetWord(idList[0]); //we want to update the first word
            List<Word> getList = await getTask;
            Word wordToUpdate = getList[0];
            wordToUpdate.Vernacular = "Good";

            bool result = await service.Update(idList[0]); //change it now
            //TODO: Assert result WriteResult has successful code

            //let's check if it worked
            getTask = service.GetWord(idList[0]);
            getList = await getTask;
            Word updatedWord = getList[0];

            Assert.AreEqual(updatedWord.Vernacular, "Good");
            Assert.AreEqual(updatedWord.Gloss, 1);
            Assert.AreEqual(updatedWord.Audio, "audio1.mp4");
            Assert.AreEqual(updatedWord.Timestamp, "1:00");
            Assert.AreEqual(updatedWord.Id, idList[0]);

        }

        [TestMethod]
        public async Task TestDelete()
        {
            //Test with empty database
            WordService service = ServiceBuilder;

            bool deleted = await service.Delete("111111111111111111111111");
            //TODO: Assert result is NotFoundResult

            Assert.ThrowsException<System.FormatException>(() => service.Delete("1"));
            //this is not a valid id, which gives us an exception

            //Test with populated database
            string[] idList = SetUpDatabase(service).Result;
            //populates the database and gives us the ids of the members

            string wordToDelete = idList[0]; 
            //we want to get rid of the first word in the database

            deleted = await service.Delete(wordToDelete);
            //TODO: Assert result is OkResult

            Task<List<Word>> getTask = service.GetWord(wordToDelete);
            List<Word> getList = await getTask;
            Word result = getList[0];

            Assert.IsNull(result); //it should be gone now

        }
    }
}
