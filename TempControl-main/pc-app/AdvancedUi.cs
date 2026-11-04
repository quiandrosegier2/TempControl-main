using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

static class AdvancedUi
{
    public static DataGridView Grid<T>(BindingList<T> data)
    {
        var grid=new DataGridView{Dock=DockStyle.Fill,DataSource=data,AutoGenerateColumns=true,AllowUserToAddRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,BackgroundColor=StudioTheme.Surface,GridColor=StudioTheme.Border,ForeColor=StudioTheme.Text,BorderStyle=BorderStyle.None,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,EnableHeadersVisualStyles=false};
        grid.DefaultCellStyle.BackColor=StudioTheme.Surface;grid.DefaultCellStyle.ForeColor=StudioTheme.Text;grid.DefaultCellStyle.SelectionBackColor=StudioTheme.Raised;grid.DefaultCellStyle.SelectionForeColor=StudioTheme.Text;grid.ColumnHeadersDefaultCellStyle.BackColor=StudioTheme.Raised;grid.ColumnHeadersDefaultCellStyle.ForeColor=StudioTheme.Text;
        grid.DataBindingComplete+=(_,_)=>
        {
            foreach(var prop in typeof(T).GetProperties())
            {
                if(!grid.Columns.Contains(prop.Name))continue;
                var column=grid.Columns[prop.Name];if(column==null)continue;
                if(prop.GetCustomAttributes(typeof(BrowsableAttribute),true).FirstOrDefault() is BrowsableAttribute{Browsable:false}){column.Visible=false;continue;}
                if(prop.PropertyType.IsEnum&&column is not DataGridViewComboBoxColumn)
                {
                    int index=column.Index;grid.Columns.Remove(column);grid.Columns.Insert(index,new DataGridViewComboBoxColumn{DataPropertyName=prop.Name,Name=prop.Name,HeaderText=prop.Name,DataSource=prop.Name=="Sensor"&&prop.PropertyType==typeof(WidgetKind)?SensorHistory.Metrics:Enum.GetValues(prop.PropertyType),FlatStyle=FlatStyle.Flat});
                }
            }
        };
        grid.DataError+=(_,e)=>{e.ThrowException=false;};return grid;
    }
    public static void Profiles(DataGridView grid,ProfileRepository repository)
    {
        void Bind()
        {
            if(!grid.Columns.Contains("ProfileId")||grid.Columns["ProfileId"] is DataGridViewComboBoxColumn)return;
            int index=grid.Columns["ProfileId"]!.Index;grid.Columns.Remove("ProfileId");
            var profiles=new[]{new ProfileEntry("legacy","Huidig ontwerp","")}.Concat(repository.List()).ToList();
            grid.Columns.Insert(index,new DataGridViewComboBoxColumn{Name="ProfileId",DataPropertyName="ProfileId",HeaderText="Profiel",DataSource=profiles,DisplayMember="Name",ValueMember="Id",FlatStyle=FlatStyle.Flat});
        }
        grid.DataBindingComplete+=(_,_)=>Bind();Bind();
    }
    public static PropertyGrid Inspector(object? value)=>new(){Dock=DockStyle.Fill,SelectedObject=value,ViewBackColor=StudioTheme.Surface,ViewForeColor=StudioTheme.Text,HelpBackColor=StudioTheme.Surface,HelpForeColor=StudioTheme.Muted,LineColor=StudioTheme.Border,CategoryForeColor=StudioTheme.Text,ToolbarVisible=false};
    public static Button Button(FlowLayoutPanel panel,string caption,Action action,Action<Exception>? error=null,bool primary=false)
    {
        var button=StudioTheme.Button(caption,primary);button.Width=Math.Max(155,TextRenderer.MeasureText(caption,button.Font).Width+28);button.Click+=(_,_)=>{try{action();}catch(Exception ex){if(error!=null)error(ex);else MessageBox.Show(panel.FindForm(),ex.Message,"Qscreen",MessageBoxButtons.OK,MessageBoxIcon.Information);}};panel.Controls.Add(button);return button;
    }
}
sealed class HistoryPlot : Control
{
    public HistorySeries? Series=null;
    public HistoryPlot(){DoubleBuffered=true;Dock=DockStyle.Fill;BackColor=StudioTheme.Surface;ForeColor=StudioTheme.Text;}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);if(Series==null){TextRenderer.DrawText(e.Graphics,"Nog geen meetgegevens. Start de achtergrondmeting.",Font,ClientRectangle,StudioTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
        SensorHistory.Draw(e.Graphics,new RectangleF(40,30,Math.Max(1,Width-80),Math.Max(1,Height-90)),Series,Color.Cyan);
        string stats=Series.Minimum==null?"Sensor niet beschikbaar":$"Minimum: {Series.Minimum:0.0}     Gemiddeld: {Series.Average:0.0}     Maximum: {Series.Maximum:0.0}";
        TextRenderer.DrawText(e.Graphics,stats,Font,new Rectangle(40,Height-42,Width-80,30),StudioTheme.Text);
    }
}
