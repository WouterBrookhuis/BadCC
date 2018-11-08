import subprocess
import os
import os.path

compiler_path = "badcc/bin/debug/badcc"

test_file_name = "write_a_c_compiler-master/stage_1/invalid/wrong_case.c"
stages_folder = "write_a_c_compiler-master"

def test_file(file_name):
    process = subprocess.Popen([compiler_path, file_name])
    process.wait()
    return process.returncode == 0

print(test_file(test_file_name))

def get_valid_tests(stage_nr):
    return os.listdir(os.path.join(stages_folder, "stage_" + str(stage_nr), "valid"))

print(get_dir_files("write_a_c_compiler-master/stage_1/invalid/"))
