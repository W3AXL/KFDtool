﻿using KFDtool.Container;
using KFDtool.Elite;
using KFDtool.Gui.Dialog;
using KFDtool.P25.TransferConstructs;
using KFDtool.Shared;
using Microsoft.Win32;
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;

namespace KFDtool.Gui.Control
{
    /// <summary>
    /// Interaction logic for P25MultipleKeyload.xaml
    /// </summary>
    public partial class P25MultipleKeyload : UserControl
    {
        private static NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private List<int> Keys;

        private List<int> Groups;

        private Dictionary<int, string> KeysAvailable;

        private Dictionary<int, string> KeysSelected;

        private Dictionary<int, string> KeksAvailable;

        private Dictionary<int, string> GroupsAvailable;

        private Dictionary<int, string> GroupsSelected;

        public P25MultipleKeyload()
        {
            InitializeComponent();

            Keys = new List<int>();

            Groups = new List<int>();

            KeysAvailable = new Dictionary<int, string>();

            KeysSelected = new Dictionary<int, string>();

            KeksAvailable = new Dictionary<int, string>();

            GroupsAvailable = new Dictionary<int, string>();

            GroupsSelected = new Dictionary<int, string>();

            lbKeysAvailable.ItemsSource = KeysAvailable;

            lbKeysSelected.ItemsSource = KeysSelected;

            dropKeksAvailable.ItemsSource = KeksAvailable;

            lbGroupsAvailable.ItemsSource = GroupsAvailable;

            lbGroupsSelected.ItemsSource = GroupsSelected;

            UpdateKeysColumns();

            UpdateKeksDropdown();

            UpdateGroupsColumns();
        }

        private void UpdateKeysColumns()
        {
            KeysAvailable.Clear();

            foreach (KeyItem keyItem in Settings.ContainerInner.Keys)
            {
                KeysAvailable.Add(keyItem.Id, keyItem.Name);
            }

            KeysSelected.Clear();

            foreach (int key in Keys)
            {
                KeysSelected.Add(key, KeysAvailable[key]);
            }

            foreach (KeyValuePair<int, string> selected in KeysSelected)
            {
                KeysAvailable.Remove(selected.Key);
            }

            lbKeysAvailable.Items.Refresh();

            lbKeysSelected.Items.Refresh();
        }

        private void UpdateKeksDropdown()
        {
            KeksAvailable.Clear();

            KeksAvailable.Add(-1, "Clear Keyload");
            dropKeksAvailable.SelectedIndex = 0;

            foreach (KeyItem keyItem in Settings.ContainerInner.Keys)
            {
                // AACA-A 6.1/Fig. 5
                if (keyItem.Sln >= 61440)
                {
                    KeksAvailable.Add(keyItem.Id, keyItem.Name);
                }
            }

            dropKeksAvailable.Items.Refresh();
        }

        private void UpdateGroupsColumns()
        {
            GroupsAvailable.Clear();

            foreach (Container.GroupItem groupItem in Settings.ContainerInner.Groups)
            {
                GroupsAvailable.Add(groupItem.Id, groupItem.Name);
            }

            GroupsSelected.Clear();

            foreach (int group in Groups)
            {
                GroupsSelected.Add(group, GroupsAvailable[group]);
            }

            foreach (KeyValuePair<int, string> selected in GroupsSelected)
            {
                GroupsAvailable.Remove(selected.Key);
            }

            lbGroupsAvailable.Items.Refresh();

            lbGroupsSelected.Items.Refresh();
        }

        private void Keys_Add_Click(object sender, RoutedEventArgs e)
        {
            if (lbKeysAvailable.SelectedItem != null)
            {
                int key = ((KeyValuePair<int, string>)lbKeysAvailable.SelectedItem).Key;

                Keys.Add(key);

                UpdateKeysColumns();
            }
        }

        private void Keys_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (lbKeysSelected.SelectedItem != null)
            {
                int key = ((KeyValuePair<int, string>)lbKeysSelected.SelectedItem).Key;

                Keys.Remove(key);

                UpdateKeysColumns();
            }
        }

        private void Groups_Add_Click(object sender, RoutedEventArgs e)
        {
            if (lbGroupsAvailable.SelectedItem != null)
            {
                int key = ((KeyValuePair<int, string>)lbGroupsAvailable.SelectedItem).Key;

                Groups.Add(key);

                UpdateGroupsColumns();
            }
        }

        private void Groups_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (lbGroupsSelected.SelectedItem != null)
            {
                int key = ((KeyValuePair<int, string>)lbGroupsSelected.SelectedItem).Key;

                Groups.Remove(key);

                UpdateGroupsColumns();
            }
        }

        private void Load()
        {
            List<int> combinedKeys = new List<int>();

            combinedKeys.AddRange(Keys);

            foreach (int groupItemId in Groups)
            {
                bool found = false;

                foreach (Container.GroupItem containerGroupItem in Settings.ContainerInner.Groups)
                {
                    if (groupItemId == containerGroupItem.Id)
                    {
                        found = true;

                        combinedKeys.AddRange(containerGroupItem.Keys);

                        break;
                    }
                }

                if (!found)
                {
                    throw new Exception(string.Format("group with id {0} not found in container", groupItemId));
                }
            }

            if (combinedKeys.Count == 0)
            {
                throw new Exception("no keys/groups selected");
            }

            List<CmdKeyItem> keys = new List<CmdKeyItem>();

            foreach (int keyId in combinedKeys)
            {
                bool found = false;

                foreach (KeyItem containerKeyItem in Settings.ContainerInner.Keys)
                {
                    if (keyId == containerKeyItem.Id)
                    {
                        found = true;

                        CmdKeyItem cmdKeyItem = new CmdKeyItem();

                        cmdKeyItem.UseActiveKeyset = containerKeyItem.ActiveKeyset;
                        cmdKeyItem.KeysetId = containerKeyItem.KeysetId;
                        cmdKeyItem.Sln = containerKeyItem.Sln;

                        if (containerKeyItem.KeyTypeAuto)
                        {
                            if (cmdKeyItem.Sln >= 0 && cmdKeyItem.Sln <= 61439)
                            {
                                cmdKeyItem.IsKek = false;
                            }
                            else if (cmdKeyItem.Sln >= 61440 && cmdKeyItem.Sln <= 65535)
                            {
                                cmdKeyItem.IsKek = true;
                            }
                            else
                            {
                                throw new Exception(string.Format("invalid Sln and KeyTypeAuto set: {0}", cmdKeyItem.Sln));
                            }
                        }
                        else if (containerKeyItem.KeyTypeTek)
                        {
                            cmdKeyItem.IsKek = false;
                        }
                        else if (containerKeyItem.KeyTypeKek)
                        {
                            cmdKeyItem.IsKek = true;
                        }
                        else
                        {
                            throw new Exception("KeyTypeAuto, KeyTypeTek, and KeyTypeKek all false");
                        }

                        cmdKeyItem.KeyId = containerKeyItem.KeyId;
                        cmdKeyItem.AlgorithmId = containerKeyItem.AlgorithmId;
                        cmdKeyItem.Key = Utility.ByteStringToByteList(containerKeyItem.Key);

                        keys.Add(cmdKeyItem);

                        break;
                    }
                }

                if (!found)
                {
                    throw new Exception(string.Format("key with id {0} not found in container", keyId));
                }
            }

            // if the combo box isn't set to Clear, then keyload with the kek
            int selKekContainerIndex = ((KeyValuePair<int, string>)dropKeksAvailable.Items[dropKeksAvailable.SelectedIndex]).Key;
            if (selKekContainerIndex > -1)
            {
                CmdKeyItem selectedKek = new CmdKeyItem();
                foreach (KeyItem containerKeyItem in Settings.ContainerInner.Keys)
                {
                    if (selKekContainerIndex == containerKeyItem.Id)
                    {
                        selectedKek.Sln = containerKeyItem.Sln;
                        selectedKek.IsKek = true;
                        selectedKek.KeyId = containerKeyItem.KeyId;
                        selectedKek.AlgorithmId = containerKeyItem.AlgorithmId;
                        selectedKek.Key = Utility.ByteStringToByteList(containerKeyItem.Key);
                    }
                }
                Interact.Keyload(Settings.SelectedDevice, keys, selectedKek);
            }
            else
            {
                Interact.Keyload(Settings.SelectedDevice, keys);
            }

            
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error -- {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Key(s) Loaded Successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<int> combinedKeys = new List<int>();

                combinedKeys.AddRange(Keys);

                foreach (int groupItemId in Groups)
                {
                    bool found = false;

                    foreach (Container.GroupItem containerGroupItem in Settings.ContainerInner.Groups)
                    {
                        if (groupItemId == containerGroupItem.Id)
                        {
                            found = true;

                            combinedKeys.AddRange(containerGroupItem.Keys);

                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new Exception(string.Format("group with id {0} not found in container", groupItemId));
                    }
                }

                if (combinedKeys.Count == 0)
                {
                    throw new Exception("no keys/groups selected");
                }

                List<KeyEntry> keys = new List<KeyEntry>();

                foreach (int keyId in combinedKeys)
                {
                    bool found = false;

                    foreach (KeyItem containerKeyItem in Settings.ContainerInner.Keys)
                    {
                        if (keyId == containerKeyItem.Id)
                        {
                            found = true;

                            KeyEntry KeyItem = new KeyEntry();

                            //KeyItem.UseActiveKeyset = containerKeyItem.ActiveKeyset;
                            KeyItem.ksetId = containerKeyItem.KeysetId;
                            KeyItem.ckrId = containerKeyItem.Sln;

                            if (containerKeyItem.KeyTypeAuto)
                            {
                                if (KeyItem.ckrId >= 0 && KeyItem.ckrId <= 61439)
                                {
                                    //
                                }
                                else if (KeyItem.ckrId >= 61440 && KeyItem.ckrId <= 65535)
                                {
                                    throw new NotImplementedException("Elite Export does not support KEKs.");
                                }
                                else
                                {
                                    throw new Exception(string.Format("invalid Sln and KeyTypeAuto set: {0}", KeyItem.ckrId));
                                }
                            }
                            else if (containerKeyItem.KeyTypeTek)
                            {
                                //
                            }
                            else if (containerKeyItem.KeyTypeKek)
                            {
                                throw new NotImplementedException("Elite Export does not support KEKs.");
                            }
                            else
                            {
                                throw new Exception("KeyTypeAuto, KeyTypeTek, and KeyTypeKek all false");
                            }

                            KeyItem.keyId = containerKeyItem.KeyId;
                            KeyItem.algId = containerKeyItem.AlgorithmId;
                            KeyItem.keyData = containerKeyItem.Key;
                            // spaces are not allowed in Excel script, so replicate behavior here:
                            KeyItem.KeyName = containerKeyItem.Name.Replace(' ', '_');

                            keys.Add(KeyItem);

                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new Exception(string.Format("key with id {0} not found in container", keyId));
                    }
                }
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Encrypted 7-Zip File (*.7z)|*.7z";
                saveFileDialog.Title = "Save KeyFile";
                saveFileDialog.ShowDialog();

                string xmlOut = KeyfileInteract.SerializeKeys(keys);

                string sevenZipFileName = saveFileDialog.FileName;
                string xmlFileName = saveFileDialog.FileName + ".xml";
                ContainerSetPassword containerSetPassword = new ContainerSetPassword();
                //containerSetPassword.Owner =; // for centering in parent window
                containerSetPassword.ShowDialog();

                if (containerSetPassword.PasswordSet)
                {
                    File.WriteAllText(xmlFileName, xmlOut);

                    KeyfileInteract.GenerateKeyfile(xmlFileName, sevenZipFileName, containerSetPassword.PasswordText);
                    // Ignore warning, this is intentional
#pragma warning disable 4014
                    cleanupKeyFile(xmlFileName);
#pragma warning restore 4014
                }
                else
                {
                    throw new Exception("Password not set.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error -- {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Key(s) Exported Successfully", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static async Task cleanupKeyFile(string xmlFileName)
        {
            await Task.Delay(5000);
            // clean up after ourselves
            File.Delete(xmlFileName);
        }
    }
}
