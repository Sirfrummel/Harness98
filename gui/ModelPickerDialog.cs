using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class ModelPickerDialog : Form
    {
        private readonly IList models;
        private readonly TextBox search;
        private readonly CheckBox acceptsImages;
        private readonly CheckBox generatesImages;
        private readonly CheckBox supportsTools;
        private readonly ListView list;
        private readonly Label countLabel;

        public ModelInfo SelectedModel;

        public ModelPickerDialog(IList availableModels, ModelInfo selected)
        {
            models = availableModels;
            SelectedModel = selected;
            Text = "Select an OpenRouter model";
            ClientSize = new Size(680, 470);
            MinimumSize = new Size(520, 360);
            StartPosition = FormStartPosition.CenterParent;

            Label searchLabel = new Label();
            searchLabel.Text = "Search:";
            searchLabel.Location = new Point(8, 12);
            searchLabel.AutoSize = true;
            Controls.Add(searchLabel);

            search = new TextBox();
            search.Location = new Point(65, 8);
            search.Size = new Size(607, 20);
            search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            search.TextChanged += new EventHandler(FilterChanged);
            Controls.Add(search);

            acceptsImages = new CheckBox();
            acceptsImages.Text = "Accepts images";
            acceptsImages.Location = new Point(8, 38);
            acceptsImages.AutoSize = true;
            acceptsImages.CheckedChanged += new EventHandler(FilterChanged);
            Controls.Add(acceptsImages);

            generatesImages = new CheckBox();
            generatesImages.Text = "Generates images";
            generatesImages.Location = new Point(125, 38);
            generatesImages.AutoSize = true;
            generatesImages.CheckedChanged += new EventHandler(FilterChanged);
            Controls.Add(generatesImages);

            supportsTools = new CheckBox();
            supportsTools.Text = "Tool use";
            supportsTools.Location = new Point(260, 38);
            supportsTools.AutoSize = true;
            supportsTools.CheckedChanged += new EventHandler(FilterChanged);
            Controls.Add(supportsTools);

            countLabel = new Label();
            countLabel.Location = new Point(500, 40);
            countLabel.Size = new Size(172, 16);
            countLabel.TextAlign = ContentAlignment.TopRight;
            countLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(countLabel);

            list = new ListView();
            list.Location = new Point(8, 62);
            list.Size = new Size(664, 360);
            list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.HideSelection = false;
            list.MultiSelect = false;
            list.Columns.Add("Model", 250);
            list.Columns.Add("Model ID", 250);
            list.Columns.Add("Capabilities", 120);
            list.DoubleClick += new EventHandler(ListDoubleClick);
            Controls.Add(list);

            Button ok = new Button();
            ok.Text = "Select";
            ok.Location = new Point(516, 432);
            ok.Size = new Size(75, 28);
            ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ok.Click += new EventHandler(SelectClicked);
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.Location = new Point(597, 432);
            cancel.Size = new Size(75, 28);
            cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
            RefreshList();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            string query = search.Text.Trim().ToLower();
            list.BeginUpdate();
            list.Items.Clear();
            for (int i = 0; i < models.Count; i++)
            {
                ModelInfo model = (ModelInfo)models[i];
                if (acceptsImages.Checked && !model.AcceptsImages) continue;
                if (generatesImages.Checked && !model.GeneratesImages) continue;
                if (supportsTools.Checked && !model.SupportsTools) continue;
                if (query.Length > 0 && model.Name.ToLower().IndexOf(query) < 0 &&
                    model.Id.ToLower().IndexOf(query) < 0) continue;

                string capabilities = "Text";
                if (model.AcceptsImages) capabilities += ", Vision";
                if (model.GeneratesImages) capabilities += ", Image out";
                if (model.SupportsTools) capabilities += ", Tools";
                ListViewItem item = new ListViewItem(model.Name);
                item.SubItems.Add(model.Id);
                item.SubItems.Add(capabilities);
                item.Tag = model;
                list.Items.Add(item);
                if (SelectedModel != null && model.Id == SelectedModel.Id)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                }
            }
            list.EndUpdate();
            countLabel.Text = list.Items.Count.ToString() + " models";
        }

        private void ListDoubleClick(object sender, EventArgs e)
        {
            ChooseSelection();
        }

        private void SelectClicked(object sender, EventArgs e)
        {
            ChooseSelection();
        }

        private void ChooseSelection()
        {
            if (list.SelectedItems.Count == 0) return;
            SelectedModel = (ModelInfo)list.SelectedItems[0].Tag;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
