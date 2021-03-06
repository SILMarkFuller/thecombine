/* Mark Fuller
 * Mongo to c# api. 
 */

using System.Collections.Generic;
using System.Linq;
using BackendFramework.ValueModels;
using BackendFramework.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using BackendFramework.Context;
using BackendFramework.Services;
using System.Threading.Tasks;
using MongoDB.Bson;
using System;

namespace BackendFramework.Services
{


    public class WordService : IWordService
    {

        private readonly IWordContext _wordDatabase;

        public WordService(IWordContext collectionSettings)
        {
            _wordDatabase = collectionSettings;
        }

        public async Task<List<Word>> GetAllWords()
        {
            return await _wordDatabase.Words.Find(_ => true).ToListAsync();
        }

        public async Task<List<Word>> GetWords(System.Linq.Expressions.Expression<Func<Word, bool>> filter)
        {
            return await _wordDatabase.Words.Find(filter).ToListAsync();
        }

        public async Task<bool> DeleteAllWords()
        {
            var deleted = await _wordDatabase.Words.DeleteManyAsync(_ => true);
            if (deleted.DeletedCount != 0)
            {
                return true;
            }
            return false;
        }

        public async Task<Word> GetWord(string identificaton)
        {
            var cursor = await _wordDatabase.Words.FindAsync(x => x.Id == identificaton);
            return cursor.ToList()[0];
        }

        public async Task<Word> Create(Word word)
        {
            await _wordDatabase.Words.InsertOneAsync(word);
            return word;

        }

        public async Task<bool> Delete(string Id)
        {
            var deleted = await _wordDatabase.Words.DeleteManyAsync(x => x.Id == Id);
            return deleted.DeletedCount > 0;
        }



        public async Task<bool> Update(string Id)
        {
            FilterDefinition<Word> filter = Builders<Word>.Filter.Eq(m => m.Id, Id);

            DeleteResult deleteResult = await _wordDatabase.Words.DeleteOneAsync(filter);

            return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
        }

        public async Task<List<Word>> GetFrontier()
        {
            return await _wordDatabase.Frontier.Find(_ => true).ToListAsync();
        }
        public async Task<Word> AddFrontier(Word word)
        {
            await _wordDatabase.Frontier.InsertOneAsync(word);
            return word;
        }

        public async Task<bool> DeleteFrontier(string Id)
        {
            var deleted = await _wordDatabase.Frontier.DeleteManyAsync(x => x.Id == Id);
            return deleted.DeletedCount > 0;
        }
    }


}