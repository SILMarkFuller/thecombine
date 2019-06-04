﻿using BackendFramework.ValueModels;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackendFramework.Interfaces
{
    public interface IWordService
    {
        Task<List<Word>> GetAllWords();
        Task<List<Word>> GetWord(string Id);
        Task<Word> Create(Word word);
        Task<bool> Update(string Id, Word word);
        Task<bool> Delete(string Id);
        Task<bool> DeleteAllWords();
    }
}