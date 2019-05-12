using System.Collections.Generic;
using System.Linq;
using SqlCipher4Unity3D;
using SQLite.Attribute;
using UnityEngine;
using UnityEngine.Scripting;


// ref : https://github.com/netpyoung/SqlCipher4Unity3D/issues/4

namespace test.test_update
{
    [Preserve]
    public class player_profile
    {
        [PrimaryKey] public string save_name { get; set; }

        public int active_player { get; set; }

        public override string ToString()
        {
            return string.Format("[level_story: save_name={0}, active_player={1}]"
                , this.save_name, this.active_player);
        }
    }

    public class test_update : MonoBehaviour
    {
        private SQLiteConnection _dbconn1; //this was initialized elsewhere

        private void Start()
        {
            string dbPath = @"Assets/test/test_update/test_update.db";
            this._dbconn1 = new SQLiteConnection(dbPath, "");
            this._dbconn1.DropTable<player_profile>();
            this._dbconn1.CreateTable<player_profile>();
            this._dbconn1.InsertAll(new[]
            {
                new player_profile
                {
                    save_name = "p1",
                    active_player = 1,
                },
                new player_profile
                {
                    save_name = "p2",
                    active_player = 1,
                },
                new player_profile
                {
                    save_name = "p3",
                    active_player = 1,
                },
            });

            foreach (player_profile x in this._dbconn1.Table<player_profile>().ToList())
            {
                Debug.Log($"before : {x}");
            }

            wannabe_set_active_player("p2");

            foreach (player_profile x in this._dbconn1.Table<player_profile>().ToList())
            {
                Debug.Log($"after : {x}");
            }
        }

        public void set_active_player(string player_name)
        {
            IEnumerable<player_profile> all_active_players = this._dbconn1.Table<player_profile>()
                    .Where<player_profile>(x => x.active_player > 0 || x.save_name == player_name).ToList()
                ;

            this._dbconn1.BeginTransaction();
            if (all_active_players != null)
                foreach (player_profile player in all_active_players)
                {
                    if (player_name.Equals(player.save_name))
                    {
                        Debug.LogWarning("Attempting to set the active_player field to: " + player.save_name);
                        player.active_player = 1;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to clear the active_player field for: " + player.save_name);
                        player.active_player = 0;
                    }

                    this._dbconn1.Update(player);
                }

            this._dbconn1.Commit();
        }

        public void wannabe_set_active_player(string player_name)
        {
            //IEnumerable<player_profile> active_players = this.dbconn1.Table<player_profile>()
            //        .Where<player_profile>(x => x.active_player > 0 || x.save_name == player_name);
            // because, SqlCipher4Unity's Linq's where result's will be delayed until tolist.

            List<player_profile> active_players = this._dbconn1.Table<player_profile>()
                    .Where<player_profile>(x => x.active_player > 0 || x.save_name == player_name).ToList();

            // UpdateAll runs Update within RunInTransaction, so it is okay to skip begintransaction/commit.
            //this.dbconn1.BeginTransaction();

            if (active_players != null)
                foreach (player_profile player in active_players)

                    //if (player_name.Equals(player))
                    if (player_name.Equals(player.save_name))
                    {
                        Debug.LogWarning($"Attempting to set the active_player field to: {player_name} -> {player}");
                        player.active_player = 1;
                    }
                    else
                    {
                        Debug.LogWarning($"Attempting to clear the active_player field for: {player_name} -> {player}");
                        player.active_player = 0;
                    }

            this._dbconn1.UpdateAll(active_players);
            //this.dbconn1.Commit();

        }
    }
}