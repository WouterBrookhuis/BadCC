import subprocess
import os
import os.path

compiler_path = "badcc/bin/debug/badcc"

stages_folder = "write_a_c_compiler-master"

def compile_file(file_name):
    process = subprocess.Popen([compiler_path, file_name])
    process.wait()
    return process.returncode == 0

def get_valid_tests(stage_nr):
    return os.listdir(os.path.join(stages_folder, "stage_" + str(stage_nr), "valid"))

def get_invalid_tests(stage_nr):
    return os.listdir(os.path.join(stages_folder, "stage_" + str(stage_nr), "invalid"))

def run_stage_tests(stage_nr):
    print("Running valid tests for stage", stage_nr)
    base_path = os.path.join(stages_folder, "stage_" + str(stage_nr), "valid")
    valid_tests = get_valid_tests(stage_nr)
    good_tests = 0
    total_tests = 0
    for test in valid_tests:
        if test.endswith(".c"):
            total_tests += 1
            test_path = os.path.join(base_path, test)
            if compile_file(test_path):
                print("Success,", test)
                good_tests += 1
            else:
                print("Failed,", test)

    print("Running invalid tests for stage", stage_nr)
    base_path = os.path.join(stages_folder, "stage_" + str(stage_nr), "invalid")
    valid_tests = get_invalid_tests(stage_nr)
    for test in valid_tests:
        if test.endswith(".c"):
            total_tests += 1
            test_path = os.path.join(base_path, test)
            if not compile_file(test_path):
                print("Success,", test)
                good_tests += 1
            else:
                print("Failed,", test)

    print("Passed", good_tests, "/", total_tests, "tests for stage", stage_nr)
#run_stage_tests(1)
#run_stage_tests(2)
#run_stage_tests(3)
run_stage_tests(4)
#run_stage_tests(5)
