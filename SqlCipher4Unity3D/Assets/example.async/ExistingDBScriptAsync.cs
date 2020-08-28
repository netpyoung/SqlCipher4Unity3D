using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace example.async
{
    public class ExistingDBScriptAsync : MonoBehaviour
    {
        public Text DebugText;

        // Use this for initialization
        private async void Start()
        {
            DataServiceAsync ds = new DataServiceAsync("existing.db");
            await ds.CreateDB();
            IEnumerable<Person> people = await ds.GetPersons().ToListAsync();
            ToConsole(people);

            people = await ds.GetPersonsNamedRoberto().ToListAsync();
            ToConsole("Searching for Roberto ...");
            ToConsole(people);

            await ds.CreatePerson();
            ToConsole("New person has been created");
            Person p = await ds.GetJohnny();
            ToConsole(p.ToString());
        }

        private void ToConsole(IEnumerable<Person> people)
        {
            foreach (Person person in people) ToConsole(person.ToString());
        }

        private void ToConsole(string msg)
        {
            this.DebugText.text += Environment.NewLine + msg;
            Debug.Log(msg);
        }
    }
}