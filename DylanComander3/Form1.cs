using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;



/*
 * Третья(вторая удалена во время теста случайно со всеми проектами) версия переписаная с нуля,
 * вместо пустышек дозапись
 * Добавлен тривью на форму
 * Запилен первичный вывод логических дисков
 * Прикручен имейджлист для отображения картинки узла(папки или файла)
 * После долгих тестов и проб ,переписан метод на развертывание узла ,за основу взят метанит 
 * Хотя если честно изначально мой поход на 60% совпадал ,в даном перед развертыванием узла 
 * мы просто прописываем дочернии узлы для дочерних узлов разворачиваемого узла если таковые имеются
 * В методе заполннения узлов изменил порядок поиска,вначале папки потом файлы для коректного отображения 
 * пока отключил показ файлов ибо в винде в проводнике он не отображаются
 * Алгоритм пашет как папо карло,изменения структуры папок адекватно отображаются 
 * Пока вместо рефреша открытие закрытие узла
 * Запилить метод фильтр по отсеканию системных папок с ограниченым доступом
 * Фильтрация подключена для файлов и папок
 * Подгрузка мелких иконок в листвью по клику на узел
 * Заменил клик по узлу на автозаполнние после разворачивания узла,стали ненужны проверки это файл или дерево...failed
 * Пока оставил подгрузку при развороте узла(потом переделаю)
 * Сделаны режимы отображения крупные иконки и малые...получить главную ноду для заполнения детального листа
 * Запилен Детальный показ содержимого папки 
 * Доделан метод обновления присвертке  развертке узла
 * Переход в папку по двойному клику на элементе листвью,и автовыделение соответсвующего узла в дереве
 * проблемв при переключении режима лист вью пропадают колонки,как получить текущий развернутый узел?
 * Решил проблемы с именем папки ,метод селектнод обращаюсь к нему
 * Запиленно контекстное меню методы Move,Moveto 
 * добавлено удаление 
 * Таки сделан медот копирования (спс микрософту) чуток допилил их метод и норм
 * Включен режим перезапись одинаковых файлов при копировании
 * Реализован механизм оключения элементов констестного меню после выполнения дествия
 * Стриптулс меню в соответсвие с ТЗ если файла то имя и размер ,если папка то только имя
 * Решена проблема папки без узла(+)
 * Добавлена проверка узла при перемещении если конечный путь это файл то уведомляем и прерываем операцию
 * Оставил возможность (шанс) повторить операцию для папки в случае неверного выбора узла
 * ВНИМАНИЕ режим удаление работает !!!Будь осторожней
 * из минусов -
 * это велосипед ,временами подторможивает,нет возможности иметь копию файла (включена перезапись при копировании)
 * если провести детальные тесты то возможно еще чтото по мелочам
 * Перемещение тоже с одинаковыми файлами плохо уживается
 * 
 * 
 */
namespace DylanComander3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Text = "DylanComander ";
        }

        //храним колекцию итемов для методов move,copy,past контекстного меню
        List<ListViewItem> Move_slic = new List<ListViewItem>();
        //список для копирования
        List<ListViewItem> Сopy_slic = new List<ListViewItem>();
        /// <summary>
        /// Получаем все узлы находящиеся внутри 
        /// </summary>
        /// <param name="node"></param>
        static void GetSubDir(TreeNode node)
        {
            node.Nodes.Clear();
            //получаем родительский узел и заполняем его дочерними
            DirectoryInfo dir = new DirectoryInfo(node.Name);
            try
            {
                foreach (DirectoryInfo elem in dir.GetDirectories()
                                               .Where(n => FileAttributes.System != (File.GetAttributes(n.FullName) &
                                                FileAttributes.System))
                                                )
    
                {
                    TreeNode siblings = new TreeNode();
                    siblings.Text = elem.Name;
                    siblings.Name = elem.FullName;
                    siblings.ImageIndex = 0;
                    siblings.SelectedImageIndex = 0;
                    node.Nodes.Add(siblings);
                }

            }
            catch(Exception ex) { }

            //получаем файлы
            try
            {
               foreach(FileInfo item in dir.GetFiles())
                {
                    TreeNode fNode = new TreeNode();
                    fNode.Text = item.Name;
                    fNode.Name = item.FullName;
                    fNode.ImageIndex = 1;
                    fNode.SelectedImageIndex = 1;
                    node.Nodes.Add(fNode);
                }



            }catch(Exception ex) { }

        }

        /// <summary>
        /// Инициализация дисков при загрузке формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            foreach(var item in DriveInfo.GetDrives())
            {
                TreeNode dNode = new TreeNode();
                dNode.Text = item.Name;
                dNode.Name = item.Name;
                treeView1.Nodes.Add(dNode);
                GetSubDir(dNode);
            }
        }


        /// <summary>
        /// Отрисовка всех узлов перед открытием
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            
            //выделяем открытый узел
            treeView1.SelectedNode = e.Node;
            e.Node.SelectedImageIndex = 2;
            for(int i=0;i<e.Node.Nodes.Count;i++)
            {
                GetSubDir(e.Node.Nodes[i]);
            }

            FillList(e.Node);
        }

        /// <summary>
        /// Заполняем листвью узлами дерева
        /// </summary>
        /// <param name="eNode"></param>
        void FillList(TreeNode eNode)
        {
            listView1.Items.Clear();
           foreach(TreeNode item in eNode.Nodes)
            {
                ListViewItem lvi = new ListViewItem(item.Text, item.ImageIndex);
                lvi.SubItems.Add(item.Name);
                if(File.Exists(item.Name))
                {
                    lvi.SubItems.Add("File");
                     
                    //размер только для файла(пока упрощено допилить формулу)
                    lvi.SubItems.Add(new FileInfo(item.Name).Length+" bytes");
                }
                else
                {
                    lvi.SubItems.Add("Folder");
                }
                listView1.Items.Add(lvi);
            }
        }



        /// <summary>
        /// Реакция на сворачивание узла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView1_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            e.Node.Nodes.Clear();
            treeView1.SelectedNode = e.Node ;
            e.Node.SelectedImageIndex = 0;
            GetSubDir(e.Node);
        }

        /// <summary>
        /// Заполнение листа после шелчка по узлу дерева
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //e.Node.Expand();
            FillList(e.Node);
        }

        /// <summary>
        /// Переход или запуск файла по двойному щелчку в листе
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

            ListViewItem elem = listView1.SelectedItems[0];
            //проверить папка или файл и запустить или перейти
            if(elem.SubItems[2].Text=="File")
            {
                Process.Start(elem.SubItems[1].Text);
            }
            else
            {
                //если родительский узел свернут ,то разорачиваем
                if(!treeView1.SelectedNode.IsExpanded)
                {
                    
                    treeView1.SelectedNode.Expand();
                    
                }
                //меняем выделеный узел на тот что мы ткнули в листе
                treeView1.SelectedNode = treeView1.Nodes.Find(elem.SubItems[1].Text, true)[0];
                //разворачиваем (необязательное действие можно отключить)
                treeView1.SelectedNode.Expand();
                //смещаем фокус чтоб выделение перешло на дерево 
                treeView1.Focus();
                //Fakir was drunk but miracle was done (0_o)

            }

        }



        /// <summary>
        /// Выход из программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Включение больших иконок
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LargeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {

            largeIconToolStripMenuItem.Checked = true;
            smallIconToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = false;
            listView1.View = View.LargeIcon;

        }

        /// <summary>
        /// Отображение маленьких иконок
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SmallIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = true;
            detailToolStripMenuItem.Checked = false;
            listView1.View = View.SmallIcon;
        }

        /// <summary>
        /// Режим Детали
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DetailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = true;
            listView1.View = View.Details;
        }


        /// <summary>
        /// Удаление выделеных элементов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(treeView1.SelectedNode.Name);
            var slvi = listView1.SelectedItems;

            foreach(ListViewItem elem in slvi )
            {
                listView1.Items.Remove(elem);
                //спорное решение
                TreeNode fNode = treeView1.Nodes.Find(elem.SubItems[1].Text,true)[0];
                treeView1.Nodes.Remove(fNode);
                //Удаление файлов и папок
                if (elem.SubItems[2].Text == "Folder")
                {
                    //или через имя узла(2 варианта в силу механики организованной мной )
                    //Directory.Delete(elem.SubItems[1].Text,true);
                    Directory.Delete(fNode.Name, true);
                }
                else
                {
                    //File.Delete(elem.SubItems[1].Text);
                    File.Delete(fNode.Name);
                }
                    
            }

        }

        /// <summary>
        /// Выделеные файлы для перемещения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            //зачищаем список
            this.Move_slic.Clear();
            if (listView1.SelectedItems.Count != 0)
            {
                //идем дурацким путем так как я замучался преобразовывать и терять итемы
                foreach (ListViewItem item in listView1.SelectedItems)
                    this.Move_slic.Add(item);
                //активируем меню
                moveToToolStripMenuItem.Enabled = true;
               
            }
            //необязательно
           // else
           // {
           //     
           //     moveToToolStripMenuItem.Enabled = false;
           // }
            
            //удаляем из листа и тривью узлы
            foreach (ListViewItem elem in listView1.SelectedItems)
            {
                listView1.Items.Remove(elem);
                TreeNode fNode = treeView1.Nodes.Find(elem.SubItems[1].Text, true)[0];
                treeView1.Nodes.Remove(fNode);
            }
            
            
        }


        /// <summary>
        /// Готовый метод от микрософта копирует папку и все вложения
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
              
                Directory.CreateDirectory(destDirName);
            }
           

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// перемещение папок и файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            //вставляем файлы
            TreeNode iNode = treeView1.SelectedNode;
            if (iNode.ImageIndex == 1) { MessageBox.Show("Перемещайте в папку ,это файл!!"); return; }
            foreach (var item in this.Move_slic)
            {
                TreeNode Push = new TreeNode();
                Push.Text = item.SubItems[0].Text;
                Push.Name = item.SubItems[1].Text;
                Push.ImageIndex = item.ImageIndex;
                Push.SelectedImageIndex = item.ImageIndex;
                iNode.Nodes.Add(Push);
                listView1.Items.Add(item);
            }
           //Не юзаем готовый метод от микрософта
           foreach(var elem in this.Move_slic)
            {
                try
                {
                    if (elem.SubItems[2].Text == "Folder")
                    {
                        Directory.Move(elem.SubItems[1].Text, iNode.Name + "//" + elem.SubItems[0].Text);
                    }
                    else
                    {
                        File.Move(elem.SubItems[1].Text, iNode.Name + "//" + elem.SubItems[0].Text);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            //зачищаем список
            this.Move_slic.Clear();
            moveToToolStripMenuItem.Enabled = false;

        }

        /// <summary>
        /// Копируемые элементы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Сopy_slic.Clear();
            if (listView1.SelectedItems.Count != 0)
            {
                //идем дурацким путем так как я замучался преобразовывать и терять итемы
                foreach (ListViewItem item in listView1.SelectedItems)
                    this.Сopy_slic.Add(item);

                pastToolStripMenuItem.Enabled = true;

            }
           

        }


        /// <summary>
        /// Вставка копируемых файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //если этот узел файл то ничего не делаем
            
            //вставляем файлы
            TreeNode iNode = treeView1.SelectedNode;
            if (iNode.ImageIndex == 1) { MessageBox.Show("Копируйте в папку ,это файл!!");return; }
            foreach (var item in this.Сopy_slic)
            {
                TreeNode Push = new TreeNode();
                Push.Text = item.SubItems[0].Text;
                Push.Name = item.SubItems[1].Text;
                Push.ImageIndex = item.ImageIndex;
                Push.SelectedImageIndex = item.ImageIndex;
                iNode.Nodes.Add(Push);
                listView1.Items.Add(item);
            }
            //Не юзаем готовый метод от микрософта
            foreach (var elem in this.Сopy_slic)
            {
                try
                {
                    
                    if (elem.SubItems[2].Text == "Folder")
                    {
                       
                        //чуток допилил чтоб корневая папка копировалась
                        DirectoryCopy(elem.SubItems[1].Text, iNode.Name+"//"+elem.SubItems[0].Text, true);
                    }
                    else
                    {
                        //Перезапись включена 

                        File.Copy(elem.SubItems[1].Text, iNode.Name + "//" + elem.SubItems[0].Text,true);
                        
                       
                    }

                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            //зачищаем список
            this.Сopy_slic.Clear();
            pastToolStripMenuItem.Enabled = false;


        }

        /// <summary>
        /// Отображение имени и размера(для файлов)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView1_MouseClick(object sender, MouseEventArgs e)
        {

            if (listView1.SelectedItems[0].SubItems[2].Text == "Folder")
            {
                toolStripStatusLabel1.Text = "Folder : " + listView1.SelectedItems[0].SubItems[0].Text + "\t";
                toolStripStatusLabel2.Visible = false;
            }
            else
            {
                toolStripStatusLabel1.Text = "File : " + listView1.SelectedItems[0].SubItems[0].Text;
                toolStripStatusLabel2.Visible = true;
                toolStripStatusLabel2.Text ="Size : "+ listView1.SelectedItems[0].SubItems[3].Text;

            }

        }

        private void AutorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Autor Autor = new Autor();
            Autor.ShowDialog();
        }
    }
}
