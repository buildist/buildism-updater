<?php
function create_zip($files = array(),$destination = '',$overwrite = false) {
  //if the zip file already exists and overwrite is false, return false
  //vars
  $valid_files = array();
  //if files were passed in...
  if(is_array($files)) {
    //cycle through each file
    foreach($files as $file) {
      //make sure the file exists
      if(file_exists($file)) {
        $valid_files[] = $file;
      }
    }
  }
  //if we have good files...
    //create the archive
    $zip = new ZipArchive();
    if($zip->open($destination,$overwrite ? ZIPARCHIVE::OVERWRITE : ZIPARCHIVE::CREATE) !== true) {
      return false;
    }
    //add the files
    foreach($valid_files as $file) {
      $zip->addFile($file,$file);
    }
    //debug
    //echo 'The zip archive contains ',$zip->numFiles,' files with a status of ',$zip->status;
    
    //close the zip -- done!
    $zip->close();
    
    //check to make sure the file exists
    return file_exists($destination);
}


$q = $_SERVER['QUERY_STRING'];
if($q == 'version')
{
	header('Content-type: text/plain');
	die(file_get_contents('version.txt'));
}
else if($_GET['x'])
{
	$files = explode(';', $_GET['x']);
	$requestmd5 = md5(file_get_contents('version.txt').$_GET['x']);
	
	$filename = 'cache/'.$requestmd5.'.zip';
	if(!file_exists($filename))
	{
		$toupdate = array();
		$filenames = array();
		for($i = 0; $i < count($files); $i++)
		{
			$file_array = explode(',', $files[$i]);
			$name = str_replace('\\', '/', $file_array[0]);
			$md5 = $file_array[1];
			if($name == '' || $name[0] == '/' || $name[1] == '.' || !file_exists('bin/'.$name))
				continue;

			$filenames[] = 'bin/'.$name;
			$currentmd5 = md5_file('bin/'.$name);
			if($currentmd5 != $md5)
				$toupdate[] = 'bin/'.$name;
		}
		$toupdate = array_merge($toupdate, look_for_new_files('bin', $filenames));
		if(count($toupdate) == 0)
			$filename = 'empty.zip';
		else
			create_zip($toupdate, $filename);
	}
	header('Location: '.$filename);
}
function look_for_new_files($dir, $oldfiles)
{
	$dh = opendir($dir);
	$newfiles = array();
	while($file = readdir($dh))
	{
		$fullname = $dir.'/'.$file;
		if(is_file($fullname))
		{
			if(!in_array($fullname, $oldfiles))
				$newfiles[] = $fullname;
		}
		else if($file != '.' && $file != '..')
			$newfiles = array_merge($newfiles, look_for_new_files($fullname, $oldfiles));
	}
	return $newfiles;
}
?>