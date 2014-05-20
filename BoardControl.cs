﻿using HREngine.API;
using HREngine.API.Utilities;
using HREngine.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HREngine.Bots
{
   public class BoardControli : Bot
   {

       PenalityManager penman = PenalityManager.Instance;

      protected override API.HRCard GetMinionByPriority(HRCard lastMinion)
      {
         var result = HRBattle.GetNextMinionByPriority(MinionPriority.LowestHealth);
         if (result != null && (lastMinion == null || lastMinion != null && lastMinion.GetEntity().GetCardId() != result.GetCardId()))
            return result.GetCard();

         return null;
      }
      
       protected override int evaluatePlayfield(Playfield p)
      {
          int retval = 0;
          retval -= p.evaluatePenality;
          retval += p.owncards.Count * 1;

          retval += p.ownMaxMana;
          retval -= p.enemyMaxMana;

          retval += p.ownMinions.Count * 10 / (p.mana + 1);
          retval -= p.enemyMinions.Count * 10 / (p.mana + 1);

              

          if (p.ownHeroHp + p.ownHeroDefence > 10)
          {
              retval += p.ownHeroHp + p.ownHeroDefence;
          }
          else
          {
              retval -= (11 - p.ownHeroHp - p.ownHeroDefence) * (11 - p.ownHeroHp - p.ownHeroDefence);
          }
          retval += -p.enemyHeroHp - p.enemyHeroDefence;

          retval += p.ownWeaponAttack;// +ownWeaponDurability;
          if (!p.enemyHeroFrozen)
          {
              retval -= p.enemyWeaponDurability*p.enemyWeaponAttack;
          }
          else
          {
              if (p.enemyWeaponDurability>=1)
              {
                  retval += 12;
              }
          }

          retval += p.owncarddraw * 5;
          retval -= p.enemycarddraw * 5;

          retval += p.ownMaxMana;

          if (p.enemyMinions.Count >= 0)
          {
              int anz = p.enemyMinions.Count;
              int owntaunt = p.ownMinions.FindAll(x => x.taunt == true).Count;
              int froggs = p.ownMinions.FindAll(x => x.name == "frog").Count;
              owntaunt -= froggs;
              if (owntaunt == 0) retval -= 10 * anz;
              retval += owntaunt * 10 - 11 * anz;
          }

          int playmobs = 0;
          foreach (Action a in p.playactions)
          {
              if (a.useability && a.card.name == "lesserheal" && ((a.enemytarget >= 10 && a.enemytarget <= 20) || a.enemytarget == 200)) retval -= 5;
              if (!a.cardplay) continue;
              if (a.card.type == CardDB.cardtype.MOB) playmobs++;
              //if (a.card.name == "arcanemissiles" && a.numEnemysBeforePlayed == 0) retval -= 10; // arkane missles on enemy hero is bad :D
              
              if (a.card.name == "flamestrike" && a.numEnemysBeforePlayed <= 2) retval -= 20;
              //save spell for all classes: (except for rouge if he has no combo)
              if (p.ownHeroName != "thief" && a.card.type == CardDB.cardtype.SPELL && (a.numEnemysBeforePlayed == 0 || a.enemytarget == 200)) retval -= 11;
              if (p.ownHeroName == "thief" && a.card.type == CardDB.cardtype.SPELL && (a.enemytarget == 200) ) retval -= 11;
          }

          int mobsInHand = 0;
          foreach (Handmanager.Handcard hc in p.owncards)
          {
              if (hc.card.type == CardDB.cardtype.MOB) mobsInHand++;
          }

          if (p.ownMinions.Count - p.enemyMinions.Count >= 4 && mobsInHand >= 1 )
          {
              retval += mobsInHand * 20;
          }

          foreach (Minion m in p.ownMinions)
          {
              retval += m.Hp * 1;
              retval += m.Angr * 2;
              retval += m.card.rarity;
              if (m.windfury) retval += m.Angr;
              if (m.divineshild) retval += 1;
              if (m.stealth) retval += 1;
              //if (m.poisonous) retval += 1;
              if (m.divineshild && m.taunt) retval += 4;
          }

          foreach (Minion m in p.enemyMinions)
          {
              retval -= m.Hp*2;
              if (!m.frozen)
              {
                  retval -= m.Angr * 2;
                  if (m.windfury) retval -= m.Angr;
              }
              retval -= m.card.rarity;
              if (m.taunt) retval -= 5;
              if (m.divineshild) retval -= 1;
              if (m.stealth) retval -= 1;
              
              if (m.poisonous) retval -= 4;

              if (penman.priorityTargets.ContainsKey(m.name) && !m.silenced) retval -= penman.priorityTargets[m.name];

              if (m.Angr >= 4) retval -= 20;
              if (m.Angr >= 7) retval -= 50;
          }

          retval -= p.enemySecretCount;
          retval -= p.lostDamage;//damage which was to high (like killing a 2/1 with an 3/3 -> => lostdamage =2
          retval -= p.lostWeaponDamage;
          if (p.ownMinions.Count == 0) retval -= 20;
          if (p.enemyMinions.Count == 0) retval += 20;
          if (p.enemyHeroHp <= 0) retval = 10000;
          //soulfire etc
          int deletecardsAtLast = 0;
          foreach (Action a in p.playactions)
          {
              if (!a.cardplay) continue;
              if (a.card.name == "soulfire" || a.card.name == "doomguard" || a.card.name == "succubus") deletecardsAtLast = 1;
              if (deletecardsAtLast == 1 && !(a.card.name == "soulfire" || a.card.name == "doomguard" || a.card.name == "succubus")) retval -= 20;
          }
          if (p.enemyHeroHp >=1 && p.ownHeroHp + p.ownHeroDefence - p.guessingHeroDamage <= 0) retval -= 1000;
          if (p.ownHeroHp <= 0) retval = -10000;

          p.value = retval;
          return retval;
      }
   
   }
}
