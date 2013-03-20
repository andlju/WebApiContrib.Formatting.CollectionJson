using System.Collections.Generic;
using System.Web.Http;
using WebApiContrib.Formatting.CollectionJson.Infrastructure;
using WebApiContrib.Formatting.CollectionJson.Models;

namespace WebApiContrib.Formatting.CollectionJson.Controllers
{
    [TypeMappedCollectionJsonFormatter(typeof(FriendDocumentReader), typeof(FriendDocumentWriter))]
    public class TypeMappedFriendsController : ApiController
    {
        private IFriendRepository repo;

        public TypeMappedFriendsController(IFriendRepository repo)
        {
            this.repo = repo;
        }

        public int Post(Friend friend)
        {
            return repo.Add(friend);
        }

        public IEnumerable<Friend> Get()
        {
            return repo.GetAll();
        }

        public Friend Get(int id)
        {
            return repo.Get(id);
        }

        public Friend Put(int id, Friend friend)
        {
            friend.Id = id;
            repo.Update(friend);
            return friend;
        }

        public void Delete(int id)
        {
            repo.Remove(id);
        }
    }
}