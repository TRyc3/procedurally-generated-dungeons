using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class Manager : MonoBehaviour{

    public Tile blue;
    public Tilemap level;

    public int width, height;
    public int size_x, size_y;
    public long[] tiles, room_tiles, life_tiles;

    void Start()
    {
        MakeLevel(8, 8, 15, 3, 4);
        PopulateTileMap();
    }

    public void MakeLevel(int x, int y, int scale, int buf, int life){
        //size_x, size_y are dimensions of generic room
        //scale defines how large the whole map is
        //buf defines the space between rooms
        //life defines the amount of space allotted for random shaping of rooms
        //use bitmaps, treating individual integers as tiles takes up too much memory
        //64 bit long used for bitmap, each bit represents whether or not to place a tile

        size_x = x;
        size_y = y;
        int buffer = buf * 2;
        int life_space = life * 2;

        width = (size_x + buffer + life_space) * scale;
        height = (size_y + buffer + life_space) * scale;

        // use bitmaps to fit large amount of data
        tiles = new long[((width * height) / 64) + 2];
        room_tiles = new long[((width * height) / 64) + 2];
        life_tiles = new long[((width * height) / 64) + 2];

        //set empty map
        for (int i = 0; i < ((width * height) / 64) + 2; i++){
            tiles[i] = 0;
            room_tiles[i] = 0;
            life_tiles[i] = 0;
        }

        //set minumum room size
        int min_size = size_x * size_y;

        //create variables to store rooms
        var partitions = new List <Pair<Pair<int, int>, Pair<int, int>>>();

        var coordinates = new Pair<Pair<int, int>, Pair<int, int>>();
        coordinates.First = new Pair <int, int>();
        coordinates.Second = new Pair <int, int>();

        //partition map and make rooms
        partition(0, 0, width, height, min_size, scale, buffer, life_space, partitions);
        build_room(partitions, min_size, buffer, life_space, size_x, size_y);
    }

    public void partition (int start_x, int start_y, int end_x, int end_y, int min_size, int scale, int buffer, int life_space, List <Pair<Pair<int, int>, Pair<int, int>>> partitions){
        //if total area is enough for two rooms greater than minumum size with seperation space, preform partitions
        if ((end_x - start_x) * (end_y - start_y) > min_size * scale * 3
            && (((end_x - start_x) - (buffer * 2)) - (life_space * 2) > size_x * 3 
            || ((end_y - start_y) - (buffer * 2)) - (life_space * 2) > size_y * 3)){
            int partition_x = 0;
            int partition_y = 0;
            int size1 = 0;
            int size2 = 0;
            int dir = 0;

            // pick random locations to partition until size requirements are fulfilled
            do {
                if (Random.Range(0, 2) % 2 == 0){
                    if (((end_x - start_x) - (buffer)) - life_space > size_x * 2){
                        dir = 'H';
                        partition_x = Random.Range(start_x + size_x, end_x);
                        partition_y = 0;
                        size1 = (partition_x - start_x) * (end_y - start_y);
                        size2 = (end_x - (partition_x + 1)) * (end_y - start_y);
                    }
                } else {
                    if (((end_y - start_y) - (buffer)) - life_space > size_y * 2){
                        dir = 'V';
                        partition_y = Random.Range(start_y + size_y, end_y);
                        partition_x = 0;
                        size1 = (partition_y - start_y) * (end_x - start_x);
                        size2 = (end_y - (partition_y + 1)) * (end_x - start_x); 
                    }
                }
            } while ((((end_x - partition_x) - buffer) - life_space < size_x 
                    || ((partition_x - start_x) - buffer) - life_space < size_x && dir == 'H')
                    || (((end_y - partition_y) - buffer) - life_space < size_x
                    || ((partition_y - start_y) - buffer) - life_space < size_x && dir == 'V')
                    || size1 < min_size || size2 < min_size || dir == 0);

            //preform partitions on two new areas
            //horizontal partition
            if (dir == 'H'){
                partition(start_x, start_y, partition_x, end_y, min_size, scale, buffer, life_space, partitions);
                partition(partition_x + 1, start_y, end_x, end_y, min_size, scale, buffer, life_space, partitions);

            //vertical partition
            } else {
                partition(start_x, start_y, end_x, partition_y, min_size, scale, buffer, life_space, partitions);
                partition(start_x, partition_y + 1, end_x, end_y, min_size, scale, buffer, life_space, partitions);
            }
        } else {
            //if area cannot be divided, add partition to list
            var coordinates = new Pair<Pair<int, int>, Pair<int, int>>();
            coordinates.First = new Pair <int, int>();
            coordinates.Second = new Pair <int, int>();

            coordinates.First.First = start_x;
            coordinates.First.Second = end_x;
            coordinates.Second.First = start_y;
            coordinates.Second.Second = end_y;

            partitions.Add(coordinates);
        }
    }

    public void build_room(List <Pair<Pair<int, int>, Pair<int, int>>> partitions, int min_size, int buffer, int life_space, int size_x, int size_y){
        //create list for pasage way cores
        List <Pair<int, int>> cores = new List <Pair<int, int>> ();
        
        for(int i = 0; i < partitions.Count; i++){
            //create room within partition range
            int start_x = Random.Range(partitions[i].First.First + (buffer / 2) + (life_space / 2), ((partitions[i].First.Second - size_x) - (buffer / 2)) - (life_space / 2));
            int start_y = Random.Range(partitions[i].Second.First + (buffer / 2) + (life_space / 2), ((partitions[i].Second.Second - size_y) - (buffer / 2)) - (life_space / 2));
            int end_x = Random.Range(start_x + size_x, ((partitions[i].First.Second - (buffer / 2)) - (life_space / 2))+ 1);
            int end_y = Random.Range(start_y + size_y, ((partitions[i].Second.Second - (buffer / 2)) - (life_space / 2)) + 1);

            // send room to tile
            for (int j = start_x; j < end_x; j++){
                for (int k = start_y; k < end_y; k++){
                    set_tile(j, k, room_tiles);
                }
            }

            //build life algorithm map
            for (int j = (start_x - (life_space / 2)) + 1; j < (end_x + (life_space / 2)) - 1; j++){
                for (int k = (start_y - (life_space / 2)) + 1; k < (end_y + (life_space / 2)) - 1; k++){
                    if (Random.Range(0,2) % 2 == 0){
                        set_tile(j, k , life_tiles);
                    }
                }
            }

            //set passage way starting point
            var coordinates = new Pair<int, int> ();
            coordinates.First = end_x - ((end_x - start_x) / 2);
            coordinates.Second = end_y - ((end_y - start_y) / 2);
            cores.Add(coordinates);
        }

        //preform life algorithm
        life(life_space);   

        //create passages
        List <Pair <Pair<int, int>, Pair<int, int>>> passages = passageways(Bowyer_Watson(cores), cores.Count);
        for (int i = 0; i < passages.Count; i++){
            int start_x ;
            int start_y;
            int end_x;
            int end_y;

            if (passages[i].First.First < passages[i].Second.First){
                start_x = passages[i].First.First;
                end_x = passages[i].Second.First;
            } else {
                end_x = passages[i].First.First;
                start_x = passages[i].Second.First;   
            }

            if (passages[i].First.Second < passages[i].Second.Second){
                start_y = passages[i].First.Second;
                end_y = passages[i].Second.Second;
            } else {
                end_y = passages[i].First.Second;
                start_y = passages[i].Second.Second;   
            }

            float slope;
            if (end_x == start_x){
                slope = Mathf.Infinity;
            } else if (end_y == start_y){
                slope = 0;
            } else {
                slope = (float)(passages[i].Second.Second - passages[i].First.Second) / (float)(passages[i].Second.First - passages[i].First.First);
            }

            float diagonal = passages[i].First.Second / passages[i].First.First;

            //send passageways to life tiles
            //in preparation for merging maps, preserve room tiles in order to maintain room core areas
            //where it is 100% safe to place objects
            if (slope >= 0 && diagonal >= 1){
                for (int j = start_x; j <= end_x; j++){
                    set_tile(j, end_y, life_tiles);
                }
                for (int j = start_y; j <= end_y; j++){
                    set_tile(start_x, j, life_tiles);
                }
            } else if (slope < 0 && diagonal >=1){
                for (int j = start_x; j <= end_x; j++){
                    set_tile(j, end_y, life_tiles);
                }
                for (int j = start_y; j <= end_y; j++){
                    set_tile(end_x, j, life_tiles);
                }
            } else if (slope >=0 && diagonal < 1){
                for (int j = start_x; j <= end_x; j++){
                    set_tile(j, start_y, life_tiles);
                }
                for (int j = start_y; j <= end_y; j++){
                    set_tile(end_x, j, life_tiles);
                }
            } else {
                for (int j = start_x; j <= end_x; j++){
                    set_tile(j, start_y, life_tiles);
                }
                for (int j = start_y; j <= end_y; j++){
                    set_tile(start_x, j, life_tiles);
                }
            }
        }

        //eliminate inaccessible rooms
        flood_fill(passages[0].First.First, passages[0].First.Second);
    }

    void life(int rounds){
        //semi pre-determined life algorithm
        //base rooms are locked, surrounding tiles need to be determined
        //discourages holes in rooms
        for (int i = 0; i < rounds; i++){
            for (int j = 0; j < width; j++){
                for (int k = 0; k < height; k++){
                    int count = count_neighbors(j, k, life_tiles) + count_neighbors(j, k, tiles);
                    if (get_tile(j, k, life_tiles)){
                        if (count < 2){
                            set_tile_off(j, k, life_tiles);
                        }
                    } else {
                        if (count >= 3){
                            set_tile(j, k , life_tiles);
                        }
                    }
                }
            }
        }
    }

    void flood_fill(int x, int y){
        List <Pair <int, int>> fill_tiles = new List<Pair <int, int>>();
        fill_tiles.Add(new Pair<int, int> (x, y));
        while (fill_tiles.Count > 0){
            if (fill_tiles[0].First > 0){
                if ((get_tile(fill_tiles[0].First - 1, fill_tiles[0].Second, life_tiles) ||
                    get_tile(fill_tiles[0].First - 1, fill_tiles[0].Second, room_tiles)) &&
                    !get_tile(fill_tiles[0].First - 1, fill_tiles[0].Second, tiles)){
                        set_tile(fill_tiles[0].First - 1, fill_tiles[0].Second, tiles);
                        fill_tiles.Add(new Pair<int, int> (fill_tiles[0].First - 1, fill_tiles[0].Second));
                }
            }   
            if (fill_tiles[0].First < width - 1){
                if ((get_tile(fill_tiles[0].First + 1, fill_tiles[0].Second, life_tiles) ||
                    get_tile(fill_tiles[0].First + 1, fill_tiles[0].Second, room_tiles)) &&
                    !get_tile(fill_tiles[0].First + 1, fill_tiles[0].Second, tiles)){
                        set_tile(fill_tiles[0].First + 1, fill_tiles[0].Second, tiles);
                        fill_tiles.Add(new Pair<int, int> (fill_tiles[0].First + 1, fill_tiles[0].Second));
                }
            }
            if (fill_tiles[0].Second > 0){
                if ((get_tile(fill_tiles[0].First, fill_tiles[0].Second - 1, life_tiles) ||
                    get_tile(fill_tiles[0].First, fill_tiles[0].Second - 1, room_tiles)) &&
                    !get_tile(fill_tiles[0].First, fill_tiles[0].Second - 1, tiles)){
                        set_tile(fill_tiles[0].First, fill_tiles[0].Second - 1, tiles);
                        fill_tiles.Add(new Pair<int, int> (fill_tiles[0].First, fill_tiles[0].Second - 1));
                }
            }   
            if (fill_tiles[0].Second < height - 1){
                if ((get_tile(fill_tiles[0].First, fill_tiles[0].Second + 1, life_tiles) ||
                    get_tile(fill_tiles[0].First, fill_tiles[0].Second + 1, room_tiles)) &&
                    !get_tile(fill_tiles[0].First, fill_tiles[0].Second + 1, tiles)){
                        set_tile(fill_tiles[0].First, fill_tiles[0].Second + 1, tiles);
                        fill_tiles.Add(new Pair<int, int> (fill_tiles[0].First, fill_tiles[0].Second + 1));
                }
            }
            fill_tiles.RemoveAt(0);
        }
    }

    List <Triangle> Bowyer_Watson(List <Pair <int, int>> cores){   
        Triangle super_triangle = new Triangle(new Pair<int, int>(0, 0), new Pair<int, int>(2 * width, 0), new Pair<int, int>(0, 2 * height));
        List <Triangle> triangles = new List <Triangle>();

        //create super triangle, large enough to contain all potential values
        triangles.Add(super_triangle);
        for (int i = 0; i < cores.Count; i++){
            List <Triangle> overlapping = new List <Triangle> ();
            for (int j = 0; j < triangles.Count; j++){
                if (triangles[j].in_range(cores[i].First, cores[i].Second)){
                    overlapping.Add(triangles[j]);
                }
            }

            //find all triangles in range of given point, convert to edges, remove duplicate edges
            List <Pair <Pair<int, int>, Pair<int, int>>> good_edges = new  List <Pair <Pair<int, int>, Pair<int, int>>>();
            for (int j = 0; j < overlapping.Count; j++){
                List <int> removal = new List <int>();
                bool insert1 = true;
                bool insert2 = true;
                bool insert3 = true;

                for(int k = 0; k < good_edges.Count; k++){
                    if ((overlapping[j].First == good_edges[k].First && overlapping[j].Second == good_edges[k].Second) ||
                        (overlapping[j].First == good_edges[k].Second && overlapping[j].Second == good_edges[k].First)){
                        insert1 = false;
                        removal.Add(k);       
                    }
                    if ((overlapping[j].First == good_edges[k].First && overlapping[j].Third == good_edges[k].Second) ||
                        (overlapping[j].First == good_edges[k].Second && overlapping[j].Third == good_edges[k].First)){
                        insert2 = false;
                        removal.Add(k);
                    }
                    if ((overlapping[j].Second == good_edges[k].First && overlapping[j].Third == good_edges[k].Second) ||
                        (overlapping[j].Second == good_edges[k].Second && overlapping[j].Third == good_edges[k].First)){
                        insert3 = false;
                        removal.Add(k);
                    }
                }

                for (int l = 0; l < removal.Count; l++){
                    good_edges.RemoveAt(removal[l] - l);
                }
                removal = null;

                if (insert1){
                    good_edges.Add(new Pair<Pair <int, int>, Pair <int, int>>(overlapping[j].First, overlapping[j].Second));
                }
                if (insert2){
                    good_edges.Add(new Pair<Pair <int, int>, Pair <int, int>>(overlapping[j].First, overlapping[j].Third));
                }
                if (insert3){
                    good_edges.Add(new Pair<Pair <int, int>, Pair <int, int>>(overlapping[j].Second, overlapping[j].Third));
                }
            }

            while (overlapping.Count > 0){
                triangles.Remove(overlapping[0]);
                overlapping.RemoveAt(0);
            }

            //create new triangles using good edges and point
            for (int j = 0; j < good_edges.Count; j++){
                Triangle new_triangle = new Triangle(good_edges[j].First, good_edges[j].Second, cores[i]);
                triangles.Add(new_triangle);
            }

            overlapping = null;
            good_edges = null;
        }
        return (triangles);
    }


    List <Pair <Pair<int, int>, Pair<int, int>>> passageways (List <Triangle> triangles, int cores){
        //take triangles and add edges to list of path, sorted by length of edge
        //from the created graph, create passageways
        List <Pair <Pair<int, int>, Pair<int, int>>> paths = new List <Pair <Pair<int, int>, Pair<int, int>>>();
        List <float> dists = new List <float> ();
        List <Pair <int, int>> verticies = new List <Pair<int, int>>();


        //for each triangle, check length of edge and insert into in order of length
        //do not insert duplicates
        for (int i = 0; i < triangles.Count; i++){
            bool inserted1 = false;
            bool inserted2 = false;
            bool inserted3 = false;

            for (int j = 0; j < paths.Count; j++){
                if (!inserted1){
                    int a = triangles[i].First.First - triangles[i].Second.First;
                    int b = triangles[i].First.Second - triangles[i].Second.Second;
                    float dist = Mathf.Sqrt((a * a) + (b * b));
                    if ((paths[j].First == triangles[i].First && paths[j].Second == triangles[i].Second)
                        || (paths[j].Second == triangles[i].First && paths[j].First == triangles[i].Second)){
                        inserted1 = true;
                    } else if (dists[j] > dist){
                        paths.Insert(j, new Pair <Pair<int, int>, Pair<int, int>> (triangles[i].First, triangles[i].Second));
                        dists.Insert(j, dist);
                        inserted1 = true;

                        bool v1 = false;
                        bool v2 = false; 
                        for (int k = 0; k < verticies.Count; k++){
                            if (verticies[k] == triangles[i].First){
                                v1 = true;
                            } else if (verticies[k] == triangles[i].Second){
                                v2 = true;
                            }
                            if (v1 && v2){
                                break;
                            }
                        }
                        if (!v1){
                            verticies.Add(triangles[i].First);
                        }
                        if (!v2){
                            verticies.Add(triangles[i].Second);
                        }
                    }
                }

                if (!inserted2){
                    int a = triangles[i].First.First - triangles[i].Third.First;
                    int b = triangles[i].First.Second - triangles[i].Third.Second;
                    float dist = Mathf.Sqrt((a * a) + (b * b));
                    if ((paths[j].First == triangles[i].First && paths[j].Second == triangles[i].Third)
                        || (paths[j].Second == triangles[i].First && paths[j].First == triangles[i].Third)){
                        inserted2 = true;
                    } else if (dists[j] > dist){
                        paths.Insert(j, new Pair <Pair<int, int>, Pair<int, int>> (triangles[i].First, triangles[i].Third));
                        dists.Insert(j, dist);
                        inserted2 = true;

                        bool v1 = false;
                        bool v2 = false; 
                        for (int k = 0; k < verticies.Count; k++){
                            if (verticies[k] == triangles[i].First){
                                v1 = true;
                            } else if (verticies[k] == triangles[i].Third){
                                v2 = true;
                            }
                            if (v1 && v2){
                                break;
                            }
                        }
                        if (!v1){
                            verticies.Add(triangles[i].First);
                        }
                        if (!v2){
                            verticies.Add(triangles[i].Third);
                        }
                    }
                }

                if (!inserted3){
                    int a = triangles[i].Second.First - triangles[i].Third.First;
                    int b = triangles[i].Second.Second - triangles[i].Third.Second;
                    float dist = Mathf.Sqrt((a * a) + (b * b));
                    if ((paths[j].First == triangles[i].Second && paths[j].Second == triangles[i].Third)
                        || (paths[j].Second == triangles[i].Second && paths[j].First == triangles[i].Third)){
                        inserted3 = true;
                    } else if (dists[j] > dist){
                        paths.Insert(j, new Pair <Pair<int, int>, Pair<int, int>> (triangles[i].Second, triangles[i].Third));
                        dists.Insert(j, dist);
                        inserted3 = true;

                        bool v1 = false;
                        bool v2 = false; 
                        for (int k = 0; k < verticies.Count; k++){
                            if (verticies[k] == triangles[i].Second){
                                v1 = true;
                            } else if (verticies[k] == triangles[i].Third){
                                v2 = true;
                            }
                            if (v1 && v2){
                                break;
                            }
                        }
                        if (!v1){
                            verticies.Add(triangles[i].Second);
                        }
                        if (!v2){                            
                            verticies.Add(triangles[i].Third);
                        }
                    }
                }

                if (inserted1 && inserted2 && inserted3){
                    break;
                }
            }

            if (!inserted1){
                int a = triangles[i].First.First - triangles[i].Second.First;
                int b = triangles[i].First.Second - triangles[i].Second.Second;
                float dist = Mathf.Sqrt((a * a) + (b * b));
                paths.Add(new Pair <Pair<int, int>, Pair<int, int>> (triangles[i].First, triangles[i].Second));
                dists.Add(dist);

                bool v1 = false;
                bool v2 = false; 
                for (int k = 0; k < verticies.Count; k++){
                    if (verticies[k] == triangles[i].First){
                        v1 = true;
                    } else if (verticies[k] == triangles[i].Second){
                        v2 = true;
                    }
                    if (v1 && v2){
                        break;
                    }
                }
                if (!v1){
                    verticies.Add(triangles[i].First);
                }
                if (!v2){
                    verticies.Add(triangles[i].Second);
                }
            }

            if (!inserted2){
                int a = triangles[i].First.First - triangles[i].Third.First;
                int b = triangles[i].First.Second - triangles[i].Third.Second;
                float dist = Mathf.Sqrt((a * a) + (b * b));
                paths.Add(new Pair <Pair<int, int>, Pair<int, int>> (triangles[i].First, triangles[i].Third));
                dists.Add(dist);

                bool v1 = false;
                bool v2 = false; 
                for (int k = 0; k < verticies.Count; k++){
                    if (verticies[k] == triangles[i].First){
                        v1 = true;
                    } else if (verticies[k] == triangles[i].Third){
                        v2 = true;
                    }
                    if (v1 && v2){
                        break;
                    }
                }
                if (!v1){
                    verticies.Add(triangles[i].First);
                }
                if (!v2){
                    verticies.Add(triangles[i].Third);
                }
            }

            if (!inserted3){
                int a = triangles[i].Second.First - triangles[i].Third.First;
                int b = triangles[i].Second.Second - triangles[i].Third.Second;
                float dist = Mathf.Sqrt((a * a) + (b * b));
                paths.Add( new Pair <Pair<int, int>, Pair<int, int>> (triangles[i].Second, triangles[i].Third));
                dists.Add(dist);

                bool v1 = false;
                bool v2 = false; 
                for (int k = 0; k < verticies.Count; k++){
                    if (verticies[k] == triangles[i].Second){
                        v1 = true;
                    } else if (verticies[k] == triangles[i].Third){
                        v2 = true;
                    }
                    if (v1 && v2){
                        break;
                    }
                }
                if (!v1){
                    verticies.Add(triangles[i].Second);
                }
                if (!v2){
                    verticies.Add(triangles[i].Third);
                }
            }
        }

        //remove invalid edges
        int count = 0;
        while (count < paths.Count){
            if ((paths[count].First.First == 0 && paths[count].First.Second == 0) ||
                (paths[count].Second.First == 0 && paths[count].Second.Second == 0)){
                    paths.Remove(paths[count]);
            } else if (paths[count].First.First > width || paths[count].First.Second > height ||
                       paths[count].Second.First > width || paths[count].Second.Second > height){
                    paths.Remove(paths[count]);
            } else {
                count++;
            }
        }

        //remove invalid verticies
        count = 0;
        while (count < verticies.Count){
            if ((verticies[count].First == 0 && verticies[count].Second == 0) ||
                verticies[count].First > width || verticies[count].Second > height){
                    verticies.Remove(verticies[count]);
            } else {
                count++;
            }
        }        

        //take list of edges and create minimum spanning tree
        List <Pair <Pair<int, int>, Pair<int, int>>> tree = new List <Pair <Pair<int, int>, Pair<int, int>>>();
        int paths_checked = 0;
        count = 0;
        for (int i = 0; i < verticies.Count - 1; i++){
            for (int j = paths_checked + 1; j < paths.Count; j++){
                List <Pair <Pair<int, int>, Pair<int, int>>> visited = new List <Pair <Pair<int, int>, Pair<int, int>>>();
                if (!cycle_check(tree, visited, paths[j])){
                    tree.Add(paths[j]);
                    paths_checked = j;
                    break;
                }
            }
        }
        return (tree);
    }


    bool cycle_check(List <Pair <Pair<int, int>, Pair<int, int>>> tree, List <Pair <Pair<int, int>, Pair<int, int>>> visited, Pair <Pair<int, int>, Pair<int, int>> edge){
        //check if edge has already been visited
        for (int i = 0; i < visited.Count; i++){
            if (((visited[i].First.First == edge.First.First && visited[i].First.Second == edge.First.Second) &&
                (visited[i].Second.First == edge.Second.First && visited[i].Second.Second == edge.Second.Second)) ||
                ((visited[i].First.First == edge.Second.First && visited[i].First.Second == edge.Second.Second) &&
                (visited[i].Second.First == edge.First.First && visited[i].Second.Second == edge.First.Second))){
                return true;
            }
        }

        //add edge to list of visited edges
        visited.Add(edge);

        //find all adjacent edges
        bool edge1 = false;
        bool edge2 = false;
        for (int i = 0; i < tree.Count; i++){
            if ((tree[i].First.First == edge.First.First && tree[i].First.Second == edge.First.Second) && 
                !(tree[i].Second.First == edge.Second.First && tree[i].Second.Second == edge.Second.Second)){
                edge1 = cycle_check(tree, visited, tree[i]);
            }
            if ((tree[i].First.First == edge.Second.First && tree[i].First.Second == edge.Second.Second) && 
                       !(tree[i].Second.First == edge.First.First && tree[i].Second.Second == edge.First.Second)){
                edge2 = cycle_check(tree, visited, tree[i]);
            }
            if (edge1 && edge2){
                break;
            }
        }

        //if there is no loop on either end, return false
        return (edge1 && edge2);
    }

    public int count_neighbors(int x, int y, long[] tiles_arr){
        int count = 0;
        if (x > 0){
            if (get_tile(x - 1, y, tiles_arr)){
                count++;
            }
        }
        if (x < width - 1){
            if (get_tile(x + 1, y, tiles_arr)){
                count++;
            }
        }
        if (y > 0){
            if (get_tile(x, y - 1, tiles_arr)){
                count++;
            }
        }
        if (y < height - 1){
            if (get_tile(x, y + 1, tiles_arr)){
                count++;
            }
        }
        return (count);
    }
    
    public void set_tile(int x, int y, long[] tiles_arr){
        if (!get_tile(x, y, tiles_arr)){
            //convert coordinates to array location
            long target = ((long) y * (long) width) + x;
            
            //find and flip target bit
            long insert = 1L << (int)(target % 64);
            tiles_arr[target / 64] = tiles_arr[target / 64] ^ insert;
        }
    }

    public void set_tile_off(int x, int y, long[] tiles_arr){
        if (get_tile(x, y, tiles_arr)){
            //convert coordinates to array location
            long target = ((long) y * (long) width) + x;
            
            //find and flip target bit
            long insert = ~0L ^ (1L << (int)(target % 64));
            tiles_arr[target / 64] = tiles_arr[target / 64] & insert;
        }
    }

    public bool get_tile(int x, int y, long[] tiles_arr){
        //convert coordinates to array location
        long target = ((long) y * (long) width) + x;

        //find target bit
        long insert = 1L << (int)(target % 64);
        long check = tiles_arr[target / 64] & insert;

        //return result
        if (check != 0){
            return true;
        } else {
            return false;
        }
    }


    public void PopulateTileMap (){
        //iterate through tiles
        for(int i = 0 ; i < ((width * height) / 64) + 1; i++){
            //pick out occupied longs
            if (tiles[i] != 0){
                long tile = tiles[i];
                for (int j = 0; j < 64; j++){
                    //find location of tiles meant to be turned on
                    if ((tile & 1L) == 1){
                        int target = (i * 64) + j;
                        int x = target % width;
                        int y = target / width;
                        level.SetTile(new Vector3Int(x, y, 0), blue);
                    }
                    tile = tile >> 1;
                }
            }
        }
    } 
}


public class Pair<T, U> {
    public Pair() {
    }

    public Pair(T first, U second) {
        this.First = first;
        this.Second = second;
    }

    public T First { get; set; }
    public U Second { get; set; }
};

public class Triangle{
    public Triangle(){}

    public Triangle(Pair<int, int> one, Pair<int, int> two, Pair <int, int> three){
        this.First = one;
        this.Second = two;
        this.Third = three;
    }

    public Pair<int, int> First { get; set; }
    public Pair<int, int> Second { get; set; }
    public Pair<int, int> Third { get; set;}


    public bool in_range(int x, int y){
        float radius, angle, h, k, a, b, c, inverse_slope_a, inverse_slope_b;
        Pair<int, int> mid_a, mid_b;
        
        //find length of sides
        a = Mathf.Sqrt(Mathf.Pow(First.First - Second.First, 2) + Mathf.Pow(First.Second - Second.Second, 2));
        b = Mathf.Sqrt(Mathf.Pow(First.First - Third.First, 2) + Mathf.Pow(First.Second - Third.Second, 2));
        c = Mathf.Sqrt(Mathf.Pow(Second.First - Third.First, 2) + Mathf.Pow(Second.Second - Third.Second, 2));

        //find radius
        angle = Mathf.Acos(((a * a) + (b * b) - (c * c)) / (2 * a * b));
        radius = c / (Mathf.Sin(angle) * 2);


        //find perpendicular bisector
        mid_a = new Pair<int, int> ((First.First + Second.First) / 2, (First.Second + Second.Second) / 2);
        mid_b = new Pair<int, int> ((First.First + Third.First) / 2, (First.Second + Third.Second) / 2);

        //find slope of perpendicular bisector
        if((First.Second - Second.Second) == 0){
            inverse_slope_a = Mathf.Infinity;
        } else if ((First.First - Second.First) == 0){
            inverse_slope_a = 0;
        } else {
            inverse_slope_a = (1 / ((float)(First.Second - Second.Second) / (float)(First.First - Second.First))) * -1;
        }

        if((First.Second - Third.Second) == 0){
            inverse_slope_b = Mathf.Infinity;
        } else if ((First.First - Third.First) == 0){
            inverse_slope_b = 0;
        } else {
            inverse_slope_b = (1 / ((float)(First.Second - Third.Second) / (float)(First.First - Third.First))) * -1;
        }

        //find points along bisectors
        Pair<Pair <int, int>, Pair<int, int>> line_a = on_line(mid_a, inverse_slope_a, radius * 2);
        Pair<Pair <int, int>, Pair<int, int>> line_b = on_line(mid_b, inverse_slope_b, radius * 2);  

        //find intersection of bisectors, or circle center
        Pair <int, int> center = LineIntersection(line_a.First, line_a.Second, line_b.First, line_b.Second);

        //check if point is within circle
        h = x - center.First;
        k = y - center.Second;

        float target = (h * h) + (k * k);
        if (target <= (Mathf.Pow(Mathf.Round(radius), 2))){
            return true;
        } else {
            return false;
        }
    }

    Pair<Pair <int, int>, Pair<int, int>> on_line(Pair <int, int> start, float slope, float dist){
        Pair<Pair <int, int>, Pair<int, int>> line = new Pair<Pair <int, int>, Pair<int, int>>();
        if (slope == 0){
            line.First = new Pair <int, int>((int)Mathf.Round(start.First + dist), (int)Mathf.Round(start.Second));
            line.Second = new Pair <int, int>((int)Mathf.Round(start.First - dist), (int)Mathf.Round(start.Second));
        } else if (slope == Mathf.Infinity){
            line.First = new Pair <int, int>((int)Mathf.Round(start.First), (int)Mathf.Round(start.Second + dist));
            line.Second = new Pair <int, int>((int)Mathf.Round(start.First), (int)Mathf.Round(start.Second - dist));
        } else {
            float dx = dist * (dist / Mathf.Sqrt(1 + (slope * slope))); 
            float dy = dx * slope;
            Pair <int, int> high = new Pair <int, int>((int)Mathf.Round(start.First + dx), (int)Mathf.Round(start.Second + dy));
            Pair <int, int> low = new Pair <int, int>((int)Mathf.Round(start.First - dx), (int)Mathf.Round(start.Second - dy));
            line.First = high;
            line.Second = low;
        }
        return (line);
    }

    Pair<int, int> LineIntersection(Pair <int, int> a, Pair <int, int> b, Pair<int, int> c, Pair <int, int> d)
    {
        //all line segments are assumed to have an intersection
        // Line AB represented as a1x + b1y = c1
        float a1 = b.Second - a.Second;
        float b1 = a.First - b.First;
        float c1 = a1*(a.First) + b1*(a.Second);
    
        // Line CD represented as a2x + b2y = c2
        float a2 = d.Second - c.Second;
        float b2 = c.First - d.First;
        float c2 = a2*(c.First)+ b2*(c.Second);
    
        float determinant = a1*b2 - a2*b1;

        float x = (b2*c1 - b1*c2)/determinant;
        float y = (a1*c2 - a2*c1)/determinant;
        return (new Pair <int, int>((int)Mathf.Round(x), (int)Mathf.Round(y)));
    }
};