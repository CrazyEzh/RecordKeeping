﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;


namespace RecordKeeping
{
    public partial class MainForm : Form
    {
        int IncomingSelected = 0;
        int OutgoingSelected = 0;
        bool Filtered = false;
        Project projectSelected = new Project();
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Settings.Load();
            if(Settings.Conncetion.State == ConnectionState.Open)
                lbStatusText.Text = "Подключена";
            else
                lbStatusText.Text = "Отключена";

            this.ReloadData();
        }
        private void ReloadProjectAutocomplete() 
        {
            var dataSource = GetAutocompleteProject();

            if(dataSource.Count < 2)
            {
                cbFilter.Visible = false;
            }
            else
            {
                cbFilter.Visible = true;
            }

            this.cbFilter.DataSource = dataSource;
            this.cbFilter.DisplayMember = "Name";
            this.cbFilter.ValueMember = "Value";
            this.cbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            if(Filtered)
            {
                cbFilter.SelectedValue = projectSelected.Value;
            }
        }

        public void ReloadData() 
        {
            DataTable dTableInc = new DataTable();
            DataTable dTableOut = new DataTable();

            dTableInc.Columns.AddRange(getColumnList());
            dTableOut.Columns.AddRange(getColumnList());

            if (dgvIncoming.Rows.Count > 0)
            {
                for (int i = 0; i < dgvIncoming.Rows.Count; i++)
                    if (dgvIncoming.Rows[i].Selected == true)
                        IncomingSelected = i;
            }
            if (dgvOutgoing.Rows.Count > 0)
            {
                for (int i = 0; i < dgvOutgoing.Rows.Count; i++)
                    if (dgvOutgoing.Rows[i].Selected == true)
                        OutgoingSelected = i;
            }

            String sqlQuery;
            String FilterQuery = "";

            if (Filtered)
            {                
                Project project = (Project)cbFilter.SelectedItem;
                FilterQuery = String.Format(" WHERE p.project={0}", projectSelected.Value);
            }
            
            
            try
            {
                sqlQuery = "SELECT * FROM Incoming p LEFT JOIN projects pr ON pr.id = p.project" + FilterQuery;
                
                SQLiteDataAdapter adapterIncoming = new SQLiteDataAdapter(sqlQuery, Settings.Conncetion);
                adapterIncoming.Fill(dTableInc);
                dTableInc.DefaultView.Sort = "RegDate ASC";
                
                dgvIncoming.DataSource = dTableInc.DefaultView;
                                
                sqlQuery = "SELECT * FROM Outgoing p LEFT JOIN projects pr ON pr.id = p.project" + FilterQuery;
                SQLiteDataAdapter adapterOutgoing = new SQLiteDataAdapter(sqlQuery, Settings.Conncetion);
                adapterOutgoing.Fill(dTableOut);
                dTableOut.DefaultView.Sort = "RegDate ASC";

                dgvOutgoing.DataSource = dTableOut.DefaultView;
                dgvIncoming.RowPrePaint += new DataGridViewRowPrePaintEventHandler(dgvInc_RowPrePaint);
                dgvOutgoing.RowPrePaint += new DataGridViewRowPrePaintEventHandler(dgvOut_RowPrePaint);
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            if (dgvIncoming.Rows.Count < IncomingSelected)
            {
                dgvIncoming.Rows[IncomingSelected].Selected = true;
                dgvIncoming.FirstDisplayedScrollingRowIndex = IncomingSelected;
            }
            if (dgvOutgoing.Rows.Count < OutgoingSelected)
            {
                dgvOutgoing.Rows[OutgoingSelected].Selected = true;
                dgvOutgoing.FirstDisplayedScrollingRowIndex = OutgoingSelected;
            }
            ReloadProjectAutocomplete();
        }

        private List<Project> GetAutocompleteProject()
        {
            List<Project> temp_list = new List<Project>();
            SQLiteCommand reader = new SQLiteCommand();
            reader.Connection = Settings.Conncetion;
            reader.CommandText = "SELECT id, project_name FROM projects";
            SQLiteDataReader rec = reader.ExecuteReader();
            temp_list.Add(new Project() { Name = "Все проекты", Value = 0 });
            while (rec.Read())
            {
                temp_list.Add(new Project() { Name = (String)rec["project_name"], Value = (long)rec["id"] });
            }
            return temp_list;
        }

        void dgvInc_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if ((e.RowIndex > -1) && (e.RowIndex < dgvIncoming.RowCount))
            {
                string mark = dgvIncoming.Rows[e.RowIndex].Cells["dgvcIncMark"].Value.ToString();
                DataGridViewCellStyle cellStyle = dgvIncoming.Rows[e.RowIndex].DefaultCellStyle;
                switch (mark)
                {
                    case "1":
                        cellStyle.BackColor = Color.MistyRose;
                        break;
                    case "2":
                        cellStyle.BackColor = Color.LemonChiffon;
                        break;
                    case "3":
                        cellStyle.BackColor = Color.Honeydew;
                        break;
                    case "4":
                        cellStyle.BackColor = Color.LightCyan;
                        break;
                    default:
                        cellStyle.BackColor = Color.Empty;
                        break;
                }
            }
        }

        void dgvOut_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if ((e.RowIndex > -1) && (e.RowIndex < dgvOutgoing.RowCount))
            {
                string mark = dgvOutgoing.Rows[e.RowIndex].Cells["dgvcOutMark"].Value.ToString();
                DataGridViewCellStyle cellStyle = dgvOutgoing.Rows[e.RowIndex].DefaultCellStyle;
                switch(mark)
                {
                    case "1":
                        cellStyle.BackColor = Color.MistyRose;
                        break;
                    case "2":
                        cellStyle.BackColor = Color.LemonChiffon;
                        break;
                    case "3":
                        cellStyle.BackColor = Color.Honeydew;
                        break;
                    case "4":
                        cellStyle.BackColor = Color.LightCyan;
                        break;
                    default:
                        cellStyle.BackColor = Color.Empty;
                        break;
                }
            }
        }

        private void btnReloadRecords_Click(object sender, EventArgs e)
        {
            this.ReloadData();

        }

        private void btnAddRecord_Click(object sender, EventArgs e)
        {
            AddEdit addEdit = new AddEdit();
            if (tcMain.SelectedTab == tabIncoming)
                addEdit.SetDirection(Directions.Incoming);
            else if (tcMain.SelectedTab == tabOutgoing)
                addEdit.SetDirection(Directions.Outgoing);
            addEdit.ShowDialog();
            this.ReloadData();
            if (tcMain.SelectedTab == tabIncoming)
            {
                int lastrow = dgvIncoming.Rows.Count - 1;
                dgvIncoming.Rows[lastrow].Selected = true;
                dgvIncoming.FirstDisplayedScrollingRowIndex = lastrow;
            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                int lastrow = dgvOutgoing.Rows.Count - 1;
                dgvOutgoing.Rows[lastrow].Selected = true;
                dgvOutgoing.FirstDisplayedScrollingRowIndex = lastrow;
            }
        }

        private void tsmView_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = new DataGridViewRow();
            MailCard card = new MailCard();
            if (tcMain.SelectedTab == tabIncoming)
            {
                row = dgvIncoming.CurrentRow;
                card.LoadData((long)row.Cells[0].Value, Directions.Incoming);
            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                row = dgvOutgoing.CurrentRow;
                card.LoadData((long)row.Cells[0].Value, Directions.Outgoing);
            }
            card.ShowDialog();            
            this.ReloadData();
        }

        private void tsmEdit_Click(object sender, EventArgs e)
        {
            Edit();
        }

        private void Edit()
        {
            DataGridViewRow row = new DataGridViewRow();
            MailBD Record = new IncomingBD();
            Directions direction = new Directions();
            int Index = 0;
            if (tcMain.SelectedTab == tabIncoming)
            {
                row = dgvIncoming.CurrentRow;
                Index = row.Index;
                Record = new IncomingBD();
                direction = Directions.Incoming;
            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                row = dgvOutgoing.CurrentRow;
                Index = row.Index;
                Record = new OutgoingBD();
                direction = Directions.Outgoing;
            }
            Record.Load((long)row.Cells[0].Value);
            AddEdit edit = new AddEdit();
            edit.Edit = true;
            edit.LoadData(Record, direction);
            edit.ShowDialog();
            this.ReloadData();

            if (tcMain.SelectedTab == tabIncoming)
            {
                dgvIncoming.Rows[Index].Selected = true;
                dgvIncoming.FirstDisplayedScrollingRowIndex = Index;
            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                dgvOutgoing.Rows[Index].Selected = true;
                dgvOutgoing.FirstDisplayedScrollingRowIndex = Index;
            }
        }

        private void tmsDelete_Click(object sender, EventArgs e)
        {
            long id;

            if (tcMain.SelectedTab == tabIncoming)
            {
                id = (long)dgvIncoming.CurrentRow.Cells[0].Value;
                DeleteRecord(Directions.Incoming, id);

            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                id = (long)dgvOutgoing.CurrentRow.Cells[0].Value;
                DeleteRecord(Directions.Outgoing, id);
            }
        }

        private void опрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void dgvIncoming_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = new DataGridViewRow();
            MailCard card = new MailCard();
            if (tcMain.SelectedTab == tabIncoming)
            {
                row = dgvIncoming.CurrentRow;
                card.LoadData((long)row.Cells[0].Value, Directions.Incoming);
            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                row = dgvOutgoing.CurrentRow;
                card.LoadData((long)row.Cells[0].Value, Directions.Outgoing);
            }
            card.Show();
        }

        private DataColumn[] getColumnList()
        {
            List<DataColumn> columnList = new List<DataColumn>();

            DataColumn colId = new DataColumn("Id");
            colId.DataType = System.Type.GetType("System.Int64");
            columnList.Add(colId);

            DataColumn colMailNumber = new DataColumn("MailNumber");
            colMailNumber.DataType = System.Type.GetType("System.String");
            columnList.Add(colMailNumber);

            DataColumn colRegDate = new DataColumn("RegDate");
            colRegDate.DataType = System.Type.GetType("System.DateTime");
            columnList.Add(colRegDate);

            DataColumn colTitle = new DataColumn("Title");
            colTitle.DataType = System.Type.GetType("System.String");
            columnList.Add(colTitle);

            DataColumn colReplyTo = new DataColumn("ReplyTo");
            colReplyTo.DataType = System.Type.GetType("System.String");
            columnList.Add(colReplyTo);

            DataColumn colReply = new DataColumn("Reply");
            colReply.DataType = System.Type.GetType("System.String");
            columnList.Add(colReply);

            DataColumn colRecitient = new DataColumn("Recipient");
            colRecitient.DataType = System.Type.GetType("System.String");
            columnList.Add(colRecitient);

            DataColumn colMailDate = new DataColumn("MailDate");
            colMailDate.DataType = System.Type.GetType("System.DateTime");
            columnList.Add(colMailDate);

            DataColumn colDescription = new DataColumn("Description");
            colDescription.DataType = System.Type.GetType("System.String");
            columnList.Add(colDescription);

            DataColumn colFiles = new DataColumn("Files");
            colFiles.DataType = System.Type.GetType("System.String");
            columnList.Add(colFiles);

            DataColumn colMark = new DataColumn("Mark");
            colMark.DataType = System.Type.GetType("System.Int32");
            columnList.Add(colMark);
            
            DataColumn projectName = new DataColumn("project_name");
            colId.DataType = System.Type.GetType("System.String");
            columnList.Add(projectName);
            
            DataColumn Id1 = new DataColumn("Id1");
            colId.DataType = System.Type.GetType("System.Int64");
            columnList.Add(Id1);

            DataColumn project = new DataColumn("project");
            colId.DataType = System.Type.GetType("System.Int64");
            columnList.Add(project);

            return columnList.ToArray();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.Conncetion.Close();
            if (!Directory.Exists("backup"))
                Directory.CreateDirectory("backup");
            String backupFileName = "backup/" + DateTime.Now.ToString("yyMMdd_HHmm_") + "db.sqlite";

            File.Copy("db.sqlite", backupFileName);

            Settings.SaveSettings();
            
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            bool find = false;
            frmSearch search = new frmSearch();
            search.ShowDialog();
            DataGridView dgv = new DataGridView();
            if (tcMain.SelectedTab == tabIncoming)
            {
                dgv = dgvIncoming;
            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                dgv = dgvOutgoing;
            }
            if (search.SearchText != "" && search.SearchText != null)
            {
                for (int i = 0; i < dgv.RowCount; i++)
                {
                    dgv.Rows[i].Selected = false;
                    if (dgv.Rows[i].Cells[1].Value.ToString().Contains(search.SearchText) || dgv.Rows[i].Cells[3].Value.ToString().Contains(search.SearchText))
                    {
                        find = true;
                        dgv.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            if (!find)
            {
                MessageBox.Show("Данные по запросу " + search.SearchText + " не найдены", "Результат поиска");
            }
        }

        private void btnAddRecord_MouseHover(object sender, EventArgs e)
        {
            ToolTip t = new ToolTip();
            t.SetToolTip(btnAddRecord, "Добавить");
        }

        private void btnReloadRecords_MouseHover(object sender, EventArgs e)
        {
            ToolTip t = new ToolTip();
            t.SetToolTip(btnReloadRecords, "Обновить");
        }

        private void btnSearch_MouseHover(object sender, EventArgs e)
        {
            ToolTip t = new ToolTip();
            t.SetToolTip(btnSearch, "Поиск");
        }
        private void btnDelete_MouseHover(object sender, EventArgs e)
        {
            ToolTip t = new ToolTip();
            t.SetToolTip(btnDelete, "Удалить");
        }
        private void btnEdit_MouseHover(object sender, EventArgs e)
        {
            ToolTip t = new ToolTip();
            t.SetToolTip(btnEdit, "Редактировать");
        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            long id;
            
            if (tcMain.SelectedTab == tabIncoming)
            {
                id = (long)dgvIncoming.CurrentRow.Cells[0].Value;
                DeleteRecord(Directions.Incoming, id);

            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                id = (long)dgvOutgoing.CurrentRow.Cells[0].Value;
                DeleteRecord(Directions.Outgoing, id);
            }
            
        }        

        private void DeleteRecord(Directions direction, long id)
        {
            MailBD Record = new IncomingBD();
            if (direction == Directions.Incoming)                
                Record = new IncomingBD();
            else if (direction == Directions.Outgoing)
                Record = new OutgoingBD();
            
            Record.Load(id);
            DialogResult result = MessageBox.Show("Вы уверенны что хотите удалить письмо " + Record.MailNumber + "?", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                Record.Delete();
            }
            this.ReloadData();
        }

        private void SetColorMark(Directions direction, long id, int color, int Index)
        {
            MailBD Record = new IncomingBD();
            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            
            if (direction == Directions.Incoming)
                Record = new IncomingBD();
            else if (direction == Directions.Outgoing)            
                Record = new OutgoingBD();

            Record.Load(id);
            if (Record.Mark == color)            
                Record.Mark = 0;
            else
                Record.Mark = color;

            Record.Update();
            this.ReloadData();
            if (direction == Directions.Incoming)
            {
                dgvIncoming.Rows[Index].Selected = true;
                dgvIncoming.FirstDisplayedScrollingRowIndex = Index;
            }
            else if (direction == Directions.Outgoing)
            {
                dgvOutgoing.Rows[Index].Selected = true;
                dgvOutgoing.FirstDisplayedScrollingRowIndex = Index;
            }
        }

        private void tsmRedMark_Click(object sender, EventArgs e)
        {
            long id;
            if (tcMain.SelectedTab == tabIncoming)
            {
                int Index = dgvIncoming.CurrentRow.Index;
                id = (long)dgvIncoming.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Incoming, id, 1, Index);

            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                int Index = dgvOutgoing.CurrentRow.Index;
                id = (long)dgvOutgoing.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Outgoing, id, 1, Index);
            }
        }

        private void tsmYellowMark_Click(object sender, EventArgs e)
        {
            long id;
            if (tcMain.SelectedTab == tabIncoming)
            {
                int Index = dgvIncoming.CurrentRow.Index;
                id = (long)dgvIncoming.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Incoming, id, 2, Index);

            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                int Index = dgvOutgoing.CurrentRow.Index;
                id = (long)dgvOutgoing.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Outgoing, id, 2, Index);
            }
        }

        private void tsmGreenMark_Click(object sender, EventArgs e)
        {
            long id;
            if (tcMain.SelectedTab == tabIncoming)
            {
                int Index = dgvIncoming.CurrentRow.Index;
                id = (long)dgvIncoming.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Incoming, id, 3, Index);

            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                int Index = dgvOutgoing.CurrentRow.Index;
                id = (long)dgvOutgoing.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Outgoing, id, 3, Index);
            }
        }

        private void tsmBlueMark_Click(object sender, EventArgs e)
        {
            long id;
            if (tcMain.SelectedTab == tabIncoming)
            {
                int Index = dgvIncoming.CurrentRow.Index;
                id = (long)dgvIncoming.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Incoming, id, 4, Index);

            }
            else if (tcMain.SelectedTab == tabOutgoing)
            {
                int Index = dgvOutgoing.CurrentRow.Index;
                id = (long)dgvOutgoing.CurrentRow.Cells[0].Value;
                SetColorMark(Directions.Outgoing, id, 4, Index);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            Edit();
        }

        private void проектыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectManager manager = new ProjectManager();
            manager.ShowDialog();
        }

        private void cbFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFilter.SelectedIndex > 0)
            {
                Filtered = true;
                projectSelected = (Project)cbFilter.SelectedItem;
            }
            else 
            {
                Filtered = false;
            }
            ReloadData();
        }
    }
}
